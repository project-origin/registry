CREATE INDEX idx_blocks_tx_range_inc
    ON blocks USING gist (int8range(from_transaction, to_transaction, '[]'));

DROP INDEX idx_blocks_tx_range;
ALTER INDEX idx_blocks_tx_range_inc RENAME TO idx_blocks_tx_range;
