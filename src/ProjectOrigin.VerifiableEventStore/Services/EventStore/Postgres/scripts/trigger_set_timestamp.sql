-- FUNCTION: public.trigger_set_timestamp()

-- DROP FUNCTION IF EXISTS public.trigger_set_timestamp();

CREATE OR REPLACE FUNCTION public.trigger_set_timestamp()
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
    OWNER TO postgres;
