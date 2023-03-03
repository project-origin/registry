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

CREATE EXTENSION IF NOT EXISTS 'uuid-ossp';