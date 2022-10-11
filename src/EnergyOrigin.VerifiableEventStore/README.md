# Layer 2 - Verifiable event store

![C4 component diagram of layer 2](/doc/layer2_verifiable_event_store/component_diagra.drawio.svg)

## Component

### Batcher

Should be configured with parameter batch_size, this is used with 2^batch_size,
which defines the maximum size of the tree, escalating an automatic publish batch when events are added.

When a batch is being publishes, a new batch to begin collecting events are created.

An event will be added to the new batch with a reference to the previous batch, referencing the previous root hash, to ensure ordering.

Hashing is done with SHA256.


## Generate Ed25519 private key

```bash
openssl genpkey -algorithm Ed25519 -out ${KEY_FILENAME}
```

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

# Tests

## Integration test with Concordium

To be able to run the integration test with Concordium,
a Concordium node and github self-hosted runner is required.

This can be achieved using the included [docker-compose](../EnergyOrigin.VerifiableEventStore.Tests/docker-compose.yaml).
One forking the repo, one must get a token from GitHub to be able to join runners.

1. Make a .env file containing the following environment variables:

```sh
GITHUB_RUNNER_TOKEN=#YOUR_GITHUB_RUNNER_TOKEN
CONCORDIUM_HOST_DIRECTORY=/var/concordium/data
```

2. Next get the docker-compose file and run it. ***Note: it takes quite a while before the node has processed all blocks and are ready. (hours)***

    The long time for the node to be ready is why this is done in this matter instead of in a GitHub workflow, since it wouldn't be feasible.

```sh
#wget https://raw.githubusercontent.com/project-origin/registry/main/src/EnergyOrigin.VerifiableEventStore.Tests/docker-compose.yaml
wget -qO- https://raw.githubusercontent.com/project-origin/registry/eventstore/integration-tests/src/EnergyOrigin.VerifiableEventStore.Tests/docker-compose.yaml | docker-compose -f - up -d
```

3. Create an Identity and Account with Concordium.

    To create these, follow the [Concordium documentation](https://developer.concordium.software/en/mainnet/net/guides/company-identities.html)
for the testnet. More info can be found [here](https://github.com/Concordium/concordium-base/blob/main/rust-bins/docs/user-cli.md#generate-a-version-0-request-for-the-version-0-identity-object
). Wait for Concordium to respond with the **id-object.json**.

4. Wait for
