using Npgsql;
using ProjectOrigin.VerifiableEventStore.Models;

namespace ProjectOrigin.VerifiableEventStore.Services.EventStore.Postgres;

public sealed class PostgresqlEventStore : IEventStore, IDisposable
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresqlEventStore(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public PostgresqlEventStore(PostgresqlEventStoreOptions storeOptions)
    {
        _dataSource = NpgsqlDataSource.Create(storeOptions.ConnectionString);
        if (storeOptions.CreateSchema)
        {
            CreateBatchesTable();
            CreateStreamsTable();
            CreateEventsTable();
            CreateAppendEventFunction();
            CreateGetBatchFunction();
        }
    }

    public static PostgresqlEventStore Create(string connectionString)
    {
        var dataSource = NpgsqlDataSource.Create(connectionString);
        return new PostgresqlEventStore(dataSource);
    }

    public void Dispose() => _dataSource.Dispose();

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
            //blockId = reader.GetString(3);
            //transactionId = reader.GetString(4);
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
        await using var command = new NpgsqlCommand("SELECT append_event($1,$2,$3)", connection)
        {
            Parameters =
            {
                new() { Value = @event.Content, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Bytea },
                new() { Value = @event.Id.EventStreamId, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Uuid },
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
            bc.CommandText = "SELECT append_event($1,$2,$3)";
            var data = new NpgsqlParameter() { Value = item.Content };
            data.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Bytea;
            bc.Parameters.Add(data);
            var stream_id = new NpgsqlParameter() { Value = item.Id.EventStreamId };
            bc.Parameters.Add(stream_id);
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

    private void CreateGetBatchFunction()
    {
        const string creatGetBatcheSql =
       @"CREATE OR REPLACE FUNCTION public.get_batch(
	event_id uuid,
	idx integer)
    RETURNS TABLE(stream_id uuid, index integer, data bytea,block_id text, transaction_id text) 
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

";
        using var cmd = _dataSource.CreateCommand(creatGetBatcheSql);
        cmd.ExecuteNonQuery();
    }

    private void CreateAppendEventFunction()
    {
        const string creatAppendEventSql =
       @"CREATE OR REPLACE FUNCTION public.append_event(
	data bytea,
	stream_id uuid,
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
		                            WHERE b.state = 1;-- AND number_of_events < 100;
		
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
			                            raise notice 'Inserting into streams';
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
									IF total_number_of_events = 100 THEN
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

ALTER FUNCTION public.append_event(bytea, uuid, integer)
    OWNER TO postgres;

";
        using var cmd = _dataSource.CreateCommand(creatAppendEventSql);
        cmd.ExecuteNonQuery();
    }

    private void CreateEventsTable()
    {
        const string creatEventsTableSql =
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
                    ON DELETE NO ACTION
                    NOT VALID,
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
        using var cmd = _dataSource.CreateCommand(creatEventsTableSql);
        cmd.ExecuteNonQuery();
    }

    private void CreateStreamsTable()
    {
        const string creatStreamsTableSql =
        @"CREATE TABLE IF NOT EXISTS public.streams
                (
                    id uuid NOT NULL,
                    version bigint NOT NULL,
                    CONSTRAINT streams_pkey PRIMARY KEY (id)
                )
                WITH (
                    OIDS = FALSE
                )
                TABLESPACE pg_default;

                ALTER TABLE IF EXISTS public.streams
                    OWNER to postgres;
                ";
        using var cmd = _dataSource.CreateCommand(creatStreamsTableSql);
        cmd.ExecuteNonQuery();
    }

    private void CreateBatchesTable()
    {
        const string creatBatchesTableSql =
        @"CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";
CREATE TABLE IF NOT EXISTS public.batches
(
    id uuid NOT NULL DEFAULT uuid_generate_v1(),
    block_id text COLLATE pg_catalog.""default"",
    transaction_id text COLLATE pg_catalog.""default"",
    number_of_events bigint DEFAULT 0,
    state smallint DEFAULT 1,
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
";
        using var cmd = _dataSource.CreateCommand(creatBatchesTableSql);
        var result = cmd.ExecuteNonQuery();
    }
}
