BEGIN;

    CREATE TABLE transactions (
        id BIGSERIAL PRIMARY KEY,
        transaction_hash bytea NOT NULL CHECK (octet_length(transaction_hash) = 32),
        stream_id uuid NOT NULL,
        stream_index integer NOT NULL,
        payload bytea NOT NULL,

        CONSTRAINT unique_stream_index UNIQUE (stream_id, stream_index)
    );

    CREATE INDEX idx_transaction_hash ON transactions USING btree (transaction_hash);

    --------------------------------------------
    -- ensure that the order of within each stream is sequential
    -- could be optimized with shadow table (stream_id, next_index)

    CREATE OR REPLACE FUNCTION check_transaction_index_order()
    RETURNS TRIGGER AS $$
    BEGIN
        IF NEW.stream_index <> (
            COALESCE(
            (SELECT MAX(stream_index) FROM transactions WHERE stream_id = NEW.stream_id),
            -1
            ) + 1
        ) THEN
            RAISE EXCEPTION 'Invalid stream index';
        END IF;

        RETURN NEW;
    END;
    $$ LANGUAGE plpgsql;

    CREATE TRIGGER transaction_index_order_trigger
        BEFORE INSERT ON transactions
        FOR EACH ROW
        EXECUTE FUNCTION check_transaction_index_order();

    --------------------------------------------
    -- prevent any changes as of now to transactions

    CREATE OR REPLACE FUNCTION transaction_disallow_changes()
    RETURNS TRIGGER AS $$
    BEGIN
        RAISE EXCEPTION 'Updating transactions not allowed';
    END;
    $$ LANGUAGE plpgsql;

    CREATE TRIGGER prevent_transaction_updates_trigger
        BEFORE UPDATE ON transactions
        FOR EACH ROW
        EXECUTE FUNCTION transaction_disallow_changes();

END;
