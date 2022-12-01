-- FUNCTION: public.get_batch(uuid, integer)

-- DROP FUNCTION IF EXISTS public.get_batch(uuid, integer);

CREATE OR REPLACE FUNCTION public.get_batch(
	event_id uuid,
	idx integer)
    RETURNS TABLE(stream_id uuid, index integer, data bytea) 
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

		                        RETURN QUERY SELECT e.stream_id, e.index, e.data FROM events as e
		                        WHERE
			                        e.batch_id = batchId;
			
	                        END;
            
            
$BODY$;

ALTER FUNCTION public.get_batch(uuid, integer)
    OWNER TO postgres;
