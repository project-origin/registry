-- FUNCTION: public.get_batch(uuid, integer)

-- DROP FUNCTION IF EXISTS public.get_batch(uuid, integer);

CREATE OR REPLACE FUNCTION public.get_batch(
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
