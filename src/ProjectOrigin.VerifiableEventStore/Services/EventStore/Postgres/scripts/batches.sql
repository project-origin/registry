-- Table: public.batches

-- DROP TABLE IF EXISTS public.batches;

CREATE TABLE IF NOT EXISTS public.batches
(
    id uuid NOT NULL DEFAULT uuid_generate_v1(),
    block_id text COLLATE pg_catalog."default",
    transaction_id text COLLATE pg_catalog."default",
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

-- DROP TRIGGER IF EXISTS set_timestamp ON public.batches;

CREATE TRIGGER set_timestamp
    BEFORE UPDATE 
    ON public.batches
    FOR EACH ROW
    EXECUTE FUNCTION public.trigger_set_timestamp();
