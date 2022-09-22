# Layer 2 - Verifiable event store

![C4 component diagram of layer 2](/doc/layer2_verifiable_event_store/component_diagra.drawio.svg)

## Component

### Batcher

Should be configured with parameter batch_size, this is used with 2^batch_size,
which defines the maximum size of the tree, escalating an automatic publish batch when events are added.

When a batch is being publishes, a new batch to begin collecting events are created.

An event will be added to the new batch with a reference to the previous batch, referencing the previous root hash, to ensure ordering.

Hashing is done with SHA256.

## Endpoints

### 1. PublishEvent

Takes a request object that has two parameters:

```protobuf
message PublishEvent {
  required string id = 1; //uuid
  required bytes content = 2; // content of the event
}
```

The reason for the id to be set from the requesters side is to ensure that the same event cannot be added twice, possibly solved by an inbox pattern.

### 2. GetMerkleProof

```protobuf
message RequestMerkleProofForEvent {
  required string id = 1; //uuid
}
```

Return a MerkleProof for the event

```protobuf
message MerkleProof {
  required string EventId = 1; // id of the event
  required bytes Event = 2; // content of the event
  required string BlockID = 3; // Blockchain block reference
  required string TransactionID = 4; // Blockchain transaction reference
  required int64 leafIndex = 5; // leaf number in the tree, zero based.
  required repeated string hashes = 6; // Hashes needed to calculate the root based on the event.
}
```

### 3. Publish root to blockchain

```protobuf
message PublishRoot {
  required bytes content = 1; //root hash of merkle tree.
}
```

```protobuf
message PublishRootResult {
  required string transactionId = 1; //Id for the transaction on the Blockchain
}
```

### 4. Get block from transaction

```protobuf
message GetBlock {
  required string transactionId = 1; //Id for the transaction on the Blockchain
}
```

```protobuf
message GetBlockResult {
  required string blockId = 1; //Id for the block containing the transaction on the Blockchain
  required bool final = 2; //True when the block is final.
}
```

### 5. Publish hash to Blockchain

Look here: https://timestamp.northstake.dk
