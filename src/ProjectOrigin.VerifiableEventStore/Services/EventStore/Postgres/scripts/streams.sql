-- Table: public.streams

-- DROP TABLE IF EXISTS public.streams;

CREATE TABLE IF NOT EXISTS public.streams
(
    id uuid NOT NULL,
    version bigint NOT NULL,
    CONSTRAINT streams_pkey PRIMARY KEY (id)
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.streams
    OWNER to postgres;
