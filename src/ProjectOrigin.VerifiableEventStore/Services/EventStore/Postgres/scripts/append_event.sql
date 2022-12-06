-- FUNCTION: public.append_event(bytea, uuid, integer, integer)

-- DROP FUNCTION IF EXISTS public.append_event(bytea, uuid, integer, integer);

CREATE OR REPLACE FUNCTION public.append_event(
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
