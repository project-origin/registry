BEGIN;

    CREATE TABLE blocks (
        block_hash bytea PRIMARY KEY CHECK (octet_length(block_hash) = 32),
        previous_header_hash bytea NOT NULL CHECK (octet_length(previous_header_hash) = 32),
        previous_publication_hash bytea NOT NULL CHECK (octet_length(previous_publication_hash) = 32),
        merkle_root_hash bytea NOT NULL CHECK (octet_length(merkle_root_hash) = 32),
        created_at timestamptz NOT NULL DEFAULT now(),
        from_transaction bigint NOT NULL,
        to_transaction bigint NOT NULL,
        publication bytea NULL,

        CONSTRAINT check_to_transaction_greater_than_from_transaction CHECK (to_transaction >= from_transaction)
    );

    --------------------------------------------
    -- ensure that transactions are only within a single block.

    CREATE OR REPLACE FUNCTION check_block_transaction_from_to()
    RETURNS TRIGGER AS $$
    BEGIN
        -- check that the from_transaction is the last transaction in the previous block + 1
        IF NEW.from_transaction <> (COALESCE((SELECT MAX(to_transaction) FROM blocks), 0) +1)
        THEN
            RAISE EXCEPTION 'Invalid from_transactions';
        END IF;

        RETURN NEW;
    END;
    $$ LANGUAGE plpgsql;

    CREATE TRIGGER block_transaction_order_trigger
        BEFORE INSERT ON blocks
        FOR EACH ROW
        EXECUTE FUNCTION check_block_transaction_from_to();


    --------------------------------------------
    -- prevent any changes as of now to blocks

    CREATE OR REPLACE FUNCTION only_allow_publication_update()
    RETURNS TRIGGER AS $$
    BEGIN
        IF TG_OP = 'UPDATE'
            AND (NEW.publication IS DISTINCT FROM OLD.publication)
            AND (NOT (NEW.block_hash IS DISTINCT FROM OLD.block_hash))
            AND (NOT (NEW.previous_header_hash IS DISTINCT FROM OLD.previous_header_hash))
            AND (NOT (NEW.previous_publication_hash IS DISTINCT FROM OLD.previous_publication_hash))
            AND (NOT (NEW.merkle_root_hash IS DISTINCT FROM OLD.merkle_root_hash))
            AND (NOT (NEW.created_at IS DISTINCT FROM OLD.created_at))
            AND (NOT (NEW.from_transaction IS DISTINCT FROM OLD.from_transaction))
            AND (NOT (NEW.publication IS DISTINCT FROM OLD.publication))
        THEN
            RAISE EXCEPTION 'Only updates to the publication column are allowed';
        END IF;

        RETURN NEW;
    END;
    $$ LANGUAGE plpgsql;

    CREATE TRIGGER prevent_block_updates_trigger
        BEFORE UPDATE ON blocks
        FOR EACH ROW
        EXECUTE FUNCTION only_allow_publication_update();

END;
