-- FUNCTION: public.batches_for_finalization()

-- DROP FUNCTION IF EXISTS public.batches_for_finalization();

CREATE OR REPLACE FUNCTION public.batches_for_finalization(
	)
    RETURNS TABLE(batch_id uuid) 
    LANGUAGE 'plpgsql'
    COST 100
    STABLE PARALLEL SAFE 
    ROWS 1000

AS $BODY$

BEGIN
	RETURN QUERY SELECT b.id FROM batches b
		WHERE b.state = 2 order by id limit 1;

END;
$BODY$;

ALTER FUNCTION public.batches_for_finalization()
    OWNER TO postgres;

