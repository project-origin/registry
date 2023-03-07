# Project-Origin
## How to run and test the registry
The registry is currently available as .devcontainer in the repository. This means that you can run the registry in a docker container with all the dependencies installed.

### Prerequisites
- [Docker](https://docs.docker.com/get-docker/)
- [VSCode](https://code.visualstudio.com/download)

### Steps
1. Clone the [repository](https://github.com/project-origin/registry/tree/main)
2. Open the repository in VSCode
3. Install the [Remote - Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) extension
4. Open the Command Palette (Ctrl+Shift+P) and select the Remote-Containers: Reopen in Container command.
5. Check the ports terminal to see which ports are available## What is Project-Origin?

Project-Origin is a Open Source project to create a **[Federated](https://arxiv.org/pdf/1202.4503.pdf) Registry**
to handle [**Granular Certificates**](concept/granular-certificates/readme.md)  (GCs). 
The GCs purpose is to prove the origin and potential conversions of energy, thus supporting the green transition and Power-to-X (PtX).

## Why Project-Origin?
The GCs purpose is to prove the origin and potential conversions of energy, thus supporting the green transition and power-to-X (PtX).

If one searches for greenwashing, there is no shortages of articles on the internet showing a growing scepticism with [the current system](https://en.energinet.dk/Energy-data/Guarantees-of-origin-el-gas-hydrogen/) for proving the origin of electricity.

Project-Origin was created because there is a need to provide a trustworthy,
**publicly verifiable** way to prove the origin of the electricity one uses on
with a high granularity. 
The project aims to enable extended use of the implementation, to other energy forms than electricity alone. 

## How it Works

To make the data **publicly verifiable**, it is required for data
to be placed somewhere that everyone can read and validate the data using unique proofs i.e. [merkleproofs](concept/unique-proofs-using-tries-merkleproofs.md).

To solve the apparent privacy issues[^1] that public verifiability would create,
the data is encrypted using [Pedersen Commitments](concept/pedersen-commitments.md),
which cannot be decrypted, but only proven.

1. Exposing users' measurement data publicly might result in undesirable and potentially damaging insights into companies' and citizens' usage.


To solve the scalability issues arising from the huge amounts of measurement data, the implementation uses a **federated setup**
instead of a fully distributed setup directly on a distributed ledger.

This is implemented as a layer-2 blockchain, where batches of data for
each registry is hashed in a merkle-tree, and each root is then written to a ledger.

This ensures immutability, while providing high throughput.

2. In Denmark alone we have 3.500.000 electricity meters,
and with hourly measurements this would create
30.660.000.000 (30 billion) measurements on a yearly basis.

More in-depth information can be found in the [architecture description](architecture/overview.md).

## What The Registry does not do?
The registry is not a PKI or a **Federated Network** infrastructure such as the following:
- [Hyperledger Firefly](https://www.hyperledger.org/projects/firefly)
- [Alchemy Supernode](https://www.alchemy.com/supernode)
- [Confidential Consortium Framework](https://ccf.microsoft.com/)

The registry is not exposing external PKI's to the network but merely acts as a validation mechanism that handles **Granular Certificates** (GCs). Preferably with an external eventstore to store the events that are used in state transitions, otherwise a local SQL database can be used with some modifications so that internal consistency is assured. 


Project-Origin was created because there is a need to provide a trustworthy,
**publicly verifiable** way to prove the origin of the electricity one uses on
with a high granularity. 
The project aims to enable extended use of the implementation, to other energy forms than electricity alone. 

## Work in Progress

The documentation and everything else is a work in progress,
so mind that everything might not be well documented yet. You can help us by explaining any documentation issues you encounter in the [discussion forum](https://github.com/orgs/project-origin/discussions/categories/documentation-issues).

## Development Status

Progress can be [tracked in the roadmap](roadmap.md).
