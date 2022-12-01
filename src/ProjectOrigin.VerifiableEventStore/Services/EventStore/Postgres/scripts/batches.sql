-- Table: public.batches

-- DROP TABLE IF EXISTS public.batches;

CREATE TABLE IF NOT EXISTS public.batches
(
    id uuid NOT NULL DEFAULT uuid_generate_v1(),
    block_id text COLLATE pg_catalog."default",
    transaction_id text COLLATE pg_catalog."default",
    open boolean NOT NULL DEFAULT true,
    number_of_events bigint DEFAULT 0,
    CONSTRAINT batches_pkey PRIMARY KEY (id),
    CONSTRAINT batch_size CHECK (number_of_events < 1000)
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.batches
    OWNER to postgres;
-- Index: open_number_of_events

-- DROP INDEX IF EXISTS public.open_number_of_events;

CREATE INDEX IF NOT EXISTS open_number_of_events
    ON public.batches USING btree
    (open ASC NULLS LAST, number_of_events ASC NULLS LAST)
    TABLESPACE pg_default;
