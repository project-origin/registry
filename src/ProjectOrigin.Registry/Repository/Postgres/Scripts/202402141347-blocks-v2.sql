BEGIN;

    CREATE TABLE blocks_v2 (
        id BIGSERIAL PRIMARY KEY,
        block_hash bytea CHECK (octet_length(block_hash) = 32),
        previous_header_hash bytea NOT NULL CHECK (octet_length(previous_header_hash) = 32),
        previous_publication_hash bytea NOT NULL CHECK (octet_length(previous_publication_hash) = 32),
        merkle_root_hash bytea NOT NULL CHECK (octet_length(merkle_root_hash) = 32),
        created_at timestamptz NOT NULL DEFAULT now(),
        from_transaction bigint NOT NULL,
        to_transaction bigint NOT NULL,
        publication bytea NULL,

        CONSTRAINT check_to_transaction_greater_than_from_transaction CHECK (to_transaction >= from_transaction)
    );

    CREATE INDEX idx_block_hash ON blocks_v2 USING btree (block_hash);

    --------------------------------------------
    -- migrate data from blocks to blocks_v2

    INSERT INTO blocks_v2 (block_hash, previous_header_hash, previous_publication_hash, merkle_root_hash, created_at, from_transaction, to_transaction, publication)
    SELECT block_hash, previous_header_hash, previous_publication_hash, merkle_root_hash, created_at, from_transaction, to_transaction, publication
    FROM blocks ORDER BY from_transaction ASC;

    --------------------------------------------
    -- ensure that transactions are only within a single block.

    CREATE TRIGGER block_transaction_order_trigger
        BEFORE INSERT ON blocks_v2
        FOR EACH ROW
        EXECUTE FUNCTION check_block_transaction_from_to();

    --------------------------------------------
    -- prevent any changes as of now to blocks

    CREATE TRIGGER prevent_block_updates_trigger
        BEFORE UPDATE ON blocks_v2
        FOR EACH ROW
        EXECUTE FUNCTION only_allow_publication_update();

    DROP TABLE blocks;
    ALTER TABLE blocks_v2 RENAME TO blocks;

END;
