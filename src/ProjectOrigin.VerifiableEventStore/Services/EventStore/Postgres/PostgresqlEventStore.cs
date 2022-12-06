using Npgsql;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres;

public sealed class PostgresqlEventStore : IEventStore, IDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly long _batchSize;

    public PostgresqlEventStore(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public PostgresqlEventStore(PostgresqlEventStoreOptions storeOptions)
    {
        _dataSource = NpgsqlDataSource.Create(storeOptions.ConnectionString);
        if (storeOptions.CreateSchema)
        {
            SetupTriggers();
            EnableExtensions();
            CreateBatchesTable();
            CreateStreamsTable();
            CreateEventsTable();
            CreateAppendEventFunction();
            CreateGetBatchFunction();
            CreateBatchesForFinalization();
        }
        _batchSize = (long)Math.Pow(2, storeOptions.BatchExponent);
    }



    public static PostgresqlEventStore Create(string connectionString)
    {
        var dataSource = NpgsqlDataSource.Create(connectionString);
        return new PostgresqlEventStore(dataSource);
    }

    public void Dispose()
    {
        _dataSource.Dispose();
    }

    public async Task<Batch?> GetBatch(EventId eventId)
    {
        await using var connection = _dataSource.CreateConnection();
        await using var command = new NpgsqlCommand("SELECT * from get_batch($1,$2)", connection)
        {
            Parameters =
            {
                new(){Value = eventId.EventStreamId},
                new(){Value = eventId.Index}
            }
        };
        await connection.OpenAsync();

        await command.PrepareAsync();
        await using var reader = await command.ExecuteReaderAsync();
        var events = new List<VerifiableEvent>();
        var blockId = string.Empty;
        var transactionId = string.Empty;
        while (await reader.ReadAsync())
        {
            var id = (Guid)reader[0];
            var index = (int)reader[1];
            var data = (byte[])reader[2];
            if (!reader.IsDBNull(3))
                blockId = reader.GetString(3);
            if (!reader.IsDBNull(4))
                transactionId = reader.GetString(4);

            var @event = new VerifiableEvent(new EventId(id, index), data);
            events.Add(@event);
        }

        return events.Count > 0 ? new Batch(blockId, transactionId, events) : null;
    }

    public async Task<IEnumerable<VerifiableEvent>> GetEventsForEventStream(Guid topic)
    {
        await using var connection = _dataSource.CreateConnection();
        await using var command = new NpgsqlCommand("SELECT stream_id, index, data FROM events where stream_id=$1", connection)
        {
            Parameters =
            {
                new(){ Value = topic}
            }
        };

        await connection.OpenAsync();
        await command.PrepareAsync();
        await using var reader = await command.ExecuteReaderAsync();
        var events = new List<VerifiableEvent>();
        while (await reader.ReadAsync())
        {
            var evt = new VerifiableEvent(new EventId(reader.GetGuid(0), reader.GetInt32(1)), (byte[])reader[2]);
            events.Add(evt);
        }
        return events;
    }

    public async Task Store(VerifiableEvent @event)
    {
        await using var connection = _dataSource.CreateConnection();
        await using var command = new NpgsqlCommand("SELECT append_event($1,$2,$3,$4)", connection)
        {
            Parameters =
            {
                new() { Value = @event.Content, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Bytea },
                new() { Value = @event.Id.EventStreamId, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Uuid },
                new() { Value = _batchSize, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer },
                new() { Value = @event.Id.Index, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer}
            }
        };

        await connection.OpenAsync();
        await command.PrepareAsync();
        var result = (bool?)await command.ExecuteScalarAsync();
        if (result.HasValue && !result.Value)
        {
            throw new OutOfOrderException();
        }
    }

    public async Task StoreBatch(Batch batch)
    {
        using var connection = _dataSource.CreateConnection();

        using var batchJob = new NpgsqlBatch(connection);
        foreach (var item in batch.Events)
        {
            var bc = batchJob.CreateBatchCommand();
            bc.CommandText = "SELECT append_event($1,$2,$3,$4)";
            var data = new NpgsqlParameter() { Value = item.Content };
            data.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Bytea;
            bc.Parameters.Add(data);
            var stream_id = new NpgsqlParameter() { Value = item.Id.EventStreamId };
            bc.Parameters.Add(stream_id);
            var batch_size = new NpgsqlParameter() { Value = _batchSize, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer };
            bc.Parameters.Add(batch_size);
            var version = new NpgsqlParameter() { Value = item.Id.Index };
            bc.Parameters.Add(version);
            batchJob.BatchCommands.Add(bc);
        }
        try
        {
            await connection.OpenAsync();
            await batchJob.PrepareAsync();
            await using var reader = await batchJob.ExecuteReaderAsync();
            await reader.ReadAsync();
            var success = (bool)reader[0];
            if (!success)
            {
                throw new OutOfOrderException();
            }
        }
        catch (Exception)
        {
            throw;
        }
        finally { await connection.CloseAsync(); }
    }

    public async Task<IEnumerable<VerifiableEvent>> GetEventsForBatch(Guid batchId)
    {
        await using var connection = _dataSource.CreateConnection();
        await using var command = new NpgsqlCommand("SELECT stream_id, index, data FROM events where batch_id=$1", connection)
        {
            Parameters =
            {
                new(){ Value = batchId }
            }
        };

        await connection.OpenAsync();
        await command.PrepareAsync();
        await using var reader = await command.ExecuteReaderAsync();
        var events = new List<VerifiableEvent>();
        while (await reader.ReadAsync())
        {
            var evt = new VerifiableEvent(new EventId(reader.GetGuid(0), reader.GetInt32(1)), (byte[])reader[2]);
            events.Add(evt);
        }
        return events;
    }

    public async Task<IEnumerable<Guid>> GetBatchesForFinalization(int numberOfBatches)
    {
        await using var connection = _dataSource.CreateConnection();
        await using var command = new NpgsqlCommand("SELECT * FROM batches_for_finalization($1)", connection)
        {
            Parameters =
            {
                new(){ Value = numberOfBatches, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer }
            }
        };
        await connection.OpenAsync();
        await command.PrepareAsync();
        await using var reader = await command.ExecuteReaderAsync();
        var batchIds = new List<Guid>();
        while (await reader.ReadAsync())
        {
            batchIds.Add(reader.GetGuid(0));
        }
        return batchIds;
    }

    public async Task FinalizeBatch(Guid batchId, string blockId, string transactionHash)
    {
        await using var connection = _dataSource.CreateConnection();
        await using var command = new NpgsqlCommand("UPDATE batches SET block_id=$1, transaction_id=$2, state=4 WHERE id=$3", connection)
        {
            Parameters =
            {
                new() { Value = blockId },
                new() { Value = transactionHash },
                new() { Value = batchId },
            }
        };
        await connection.OpenAsync();
        await command.PrepareAsync();

        await command.ExecuteNonQueryAsync();
    }

    private void EnableExtensions()
    {
        const string sql =
            @"CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";";
        using var cmd = _dataSource.CreateCommand(sql);
        cmd.ExecuteNonQuery();
    }

    private void SetupTriggers()
    {
        const string sql = @"CREATE OR REPLACE FUNCTION public.trigger_set_timestamp()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS $BODY$
BEGIN
  NEW.updated_at = NOW();
  RETURN NEW;
END;
$BODY$;

ALTER FUNCTION public.trigger_set_timestamp()
    OWNER TO postgres;";
        using var cmd = _dataSource.CreateCommand(sql);
        cmd.ExecuteNonQuery();
    }

    private void CreateGetBatchFunction()
    {
        const string sql =
       @"CREATE OR REPLACE FUNCTION public.get_batch(
	event_id uuid,
	idx integer)
    RETURNS TABLE(stream_id uuid, index integer, data bytea, block_id text, transaction_id text) 
    LANGUAGE 'plpgsql'
    COST 100
    STABLE PARALLEL SAFE 
    ROWS 1000

AS $BODY$


DECLARE
	batchId uuid;
BEGIN
	SELECT e.batch_id INTO batchId
	FROM events as e
	WHERE
		e.stream_id = event_id and e.index = idx;

	RETURN QUERY SELECT b.id, e.index, e.data,b.block_id,b.transaction_id FROM batches b
		JOIN events e
		on e.batch_id = b.id
		WHERE b.id = batchId;

END;
$BODY$;

ALTER FUNCTION public.get_batch(uuid, integer)
    OWNER TO postgres;
";
        using var cmd = _dataSource.CreateCommand(sql);
        cmd.ExecuteNonQuery();
    }

    private void CreateAppendEventFunction()
    {
        const string sql =
       @"CREATE OR REPLACE FUNCTION public.append_event(
	data bytea,
	stream_id uuid,
	batch_size integer,
	expected_stream_version integer DEFAULT NULL::bigint)
    RETURNS boolean
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$


                            DECLARE
	                            current_batch uuid;
	                            total_number_of_events bigint;
	                            stream_version int;
								batches_state smallint;
	                            BEGIN
		                            -- Get current batch
		                            SELECT b.id, b.number_of_events INTO current_batch, total_number_of_events
		                            FROM batches b
		                            WHERE b.state = 1;

		                            IF current_batch is NULL THEN
			                            current_batch := uuid_generate_v1();
			                            total_number_of_events := 0;
			                            INSERT INTO batches(id)
			                            VALUES (current_batch);
		                            END IF;

		                            -- get stream version
		                            SELECT
			                            version INTO stream_version
		                            FROM streams as s
		                            WHERE
			                            s.id = stream_id FOR UPDATE;
		                            -- if stream doesn't exist - create new one with version 0
		                            IF stream_version IS NULL THEN
			                            stream_version := 0;
			                            INSERT INTO streams
				                            (id, version)
			                            VALUES
				                            (stream_id, stream_version);
		                            END IF;

		                            -- check optimistic concurrency
		                            IF expected_stream_version IS NOT NULL AND stream_version != expected_stream_version THEN
			                            RETURN FALSE;
		                            END IF;
		                            -- insert event
		                            INSERT INTO events(stream_id, data, index, batch_id)
		                            VALUES (stream_id, data, stream_version, current_batch);

		                            -- update batches
									total_number_of_events := total_number_of_events + 1;
									batches_state = 1;
									IF total_number_of_events = batch_size THEN
										batches_state = 2;
									END IF;
		                            UPDATE batches as b
			                            SET number_of_events = total_number_of_events, state = batches_state
		                            WHERE
			                            b.id = current_batch;

		                            -- update stream
		                            stream_version := stream_version +1;
		                            UPDATE streams as s
			                            SET version = stream_version
		                            WHERE
			                            s.id = stream_id;
		                            RETURN TRUE;
	                            END;

$BODY$;

ALTER FUNCTION public.append_event(bytea, uuid, integer, integer)
    OWNER TO postgres;

";
        using var cmd = _dataSource.CreateCommand(sql);
        cmd.ExecuteNonQuery();
    }

    private void CreateEventsTable()
    {
        const string sql =
        @"CREATE TABLE IF NOT EXISTS public.events
(
    id uuid NOT NULL DEFAULT uuid_generate_v1(),
    stream_id uuid NOT NULL,
    data bytea NOT NULL,
    index integer NOT NULL,
    batch_id uuid NOT NULL,
    created_at timestamp with time zone DEFAULT now(),
    CONSTRAINT events_pkey PRIMARY KEY (id),
    CONSTRAINT batch_id FOREIGN KEY (batch_id)
        REFERENCES public.batches (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT stream_id FOREIGN KEY (stream_id)
        REFERENCES public.streams (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.events
    OWNER to postgres;
-- Index: fki_batch_id

-- DROP INDEX IF EXISTS public.fki_batch_id;

CREATE INDEX IF NOT EXISTS fki_batch_id
    ON public.events USING btree
    (batch_id ASC NULLS LAST)
    TABLESPACE pg_default;
-- Index: fki_stream_id

-- DROP INDEX IF EXISTS public.fki_stream_id;

CREATE INDEX IF NOT EXISTS fki_stream_id
    ON public.events USING btree
    (stream_id ASC NULLS LAST)
    TABLESPACE pg_default;
-- Index: stream_id_and_version_incl

-- DROP INDEX IF EXISTS public.stream_id_and_version_incl;

CREATE INDEX IF NOT EXISTS stream_id_and_version_incl
    ON public.events USING btree
    (stream_id ASC NULLS LAST, index ASC NULLS LAST)
    TABLESPACE pg_default;
";
        using var cmd = _dataSource.CreateCommand(sql);
        cmd.ExecuteNonQuery();
    }

    private void CreateStreamsTable()
    {
        const string sql =
        @"CREATE TABLE IF NOT EXISTS public.streams
(
    id uuid NOT NULL,
    version integer NOT NULL,
    created_at timestamp with time zone DEFAULT now(),
    updated_at timestamp with time zone,
    CONSTRAINT streams_pkey PRIMARY KEY (id)
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.streams
    OWNER to postgres;

-- Trigger: set_timestamp

DROP TRIGGER IF EXISTS set_timestamp ON public.streams;

CREATE TRIGGER set_timestamp
    BEFORE UPDATE 
    ON public.streams
    FOR EACH ROW
    EXECUTE FUNCTION public.trigger_set_timestamp();

                ";
        using var cmd = _dataSource.CreateCommand(sql);
        cmd.ExecuteNonQuery();
    }

    private void CreateBatchesTable()
    {
        const string sql =
        @"
CREATE TABLE IF NOT EXISTS public.batches
(
    id uuid NOT NULL DEFAULT uuid_generate_v1(),
    block_id text COLLATE pg_catalog.""default"",
    transaction_id text COLLATE pg_catalog.""default"",
    number_of_events bigint DEFAULT 0,
    state smallint DEFAULT 1,
    created_at timestamp with time zone DEFAULT now(),
    updated_at timestamp with time zone,
    CONSTRAINT batches_pkey PRIMARY KEY (id)
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.batches
    OWNER to postgres;
-- Index: idx_state

-- DROP INDEX IF EXISTS public.idx_state;

CREATE INDEX IF NOT EXISTS idx_state
    ON public.batches USING btree
    (state ASC NULLS LAST)
    TABLESPACE pg_default;

-- Trigger: set_timestamp

DROP TRIGGER IF EXISTS set_timestamp ON public.batches;

CREATE TRIGGER set_timestamp
    BEFORE UPDATE 
    ON public.batches
    FOR EACH ROW
    EXECUTE FUNCTION public.trigger_set_timestamp();

";
        using var cmd = _dataSource.CreateCommand(sql);
        var result = cmd.ExecuteNonQuery();
    }

    private void CreateBatchesForFinalization()
    {
        const string sql =
@"CREATE OR REPLACE FUNCTION public.batches_for_finalization(
	number_of_batches integer)
    RETURNS TABLE(batch_id uuid) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$


BEGIN

	-- Select the batchid´s
	CREATE TEMP TABLE IF NOT EXISTS temp_table AS
    SELECT id
    FROM batches b
	where b.state=2
	order by id
	limit number_of_batches;

	-- Update the batches to Publishing
	UPDATE batches AS b
	SET state=3
	WHERE b.id in(SELECT id from temp_table);

	RETURN QUERY
	SELECT id
	from temp_table;
DROP TABLE temp_table;

END;
$BODY$;

ALTER FUNCTION public.batches_for_finalization(integer)
    OWNER TO postgres;

";
        using var cmd = _dataSource.CreateCommand(sql);
        var result = cmd.ExecuteNonQuery();
    }
}
