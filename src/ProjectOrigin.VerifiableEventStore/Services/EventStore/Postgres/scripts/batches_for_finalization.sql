-- FUNCTION: public.batches_for_finalization(integer)

-- DROP FUNCTION IF EXISTS public.batches_for_finalization(integer);

CREATE OR REPLACE FUNCTION public.batches_for_finalization(
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
	
--	RETURN QUERY SELECT b.id FROM batches b
--		WHERE b.state = 2 order by id limit number_of_batches;

END;
$BODY$;

ALTER FUNCTION public.batches_for_finalization(integer)
    OWNER TO postgres;
