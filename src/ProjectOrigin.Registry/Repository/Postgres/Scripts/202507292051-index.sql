CREATE INDEX idx_blocks_tx_range ON blocks USING gist (int8range(from_transaction, to_transaction));
