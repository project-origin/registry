-- Table: public.streams

-- DROP TABLE IF EXISTS public.streams;

CREATE TABLE IF NOT EXISTS public.streams
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

-- DROP TRIGGER IF EXISTS set_timestamp ON public.streams;

CREATE TRIGGER set_timestamp
    BEFORE UPDATE 
    ON public.streams
    FOR EACH ROW
    EXECUTE FUNCTION public.trigger_set_timestamp();
