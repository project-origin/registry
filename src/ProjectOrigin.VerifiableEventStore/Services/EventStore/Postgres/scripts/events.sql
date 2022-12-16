-- Table: public.events

-- DROP TABLE IF EXISTS public.events;

CREATE TABLE IF NOT EXISTS public.events
(
    id uuid NOT NULL DEFAULT uuid_generate_v1(),
    stream_id uuid NOT NULL,
    data bytea NOT NULL,
    index integer NOT NULL,
    batch_id uuid NOT NULL,
    created_at timestamp with time zone DEFAULT now(),
    CONSTRAINT events_pkey PRIMARY KEY (id),
    CONSTRAINT batch_id FOREIGN KEY (batch_id)
        REFERENCES public.batches (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT stream_id FOREIGN KEY (stream_id)
        REFERENCES public.streams (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.events
    OWNER to postgres;
-- Index: fki_batch_id

-- DROP INDEX IF EXISTS public.fki_batch_id;

CREATE INDEX IF NOT EXISTS fki_batch_id
    ON public.events USING btree
    (batch_id ASC NULLS LAST)
    TABLESPACE pg_default;
-- Index: fki_stream_id

-- DROP INDEX IF EXISTS public.fki_stream_id;

CREATE INDEX IF NOT EXISTS fki_stream_id
    ON public.events USING btree
    (stream_id ASC NULLS LAST)
    TABLESPACE pg_default;
-- Index: stream_id_and_version_incl

-- DROP INDEX IF EXISTS public.stream_id_and_version_incl;

CREATE INDEX IF NOT EXISTS stream_id_and_version_incl
    ON public.events USING btree
    (stream_id ASC NULLS LAST, index ASC NULLS LAST)
    TABLESPACE pg_default;
