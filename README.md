# Project-Origin

## Navigation
- [Documentation](https://project-origin.github.io/registry/)
- [Code](https://github.com/project-origin/registry/tree/main/src)
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
5. Check the ports terminal to see which ports are available

## Why Project-Origin?

The GCs purpose is to prove the origin and potential conversions of energy, thus supporting the green transition and power-to-X (PtX).

If one searches for greenwashing, there is no shortages of articles on the internet showing a growing scepticism with [the current system](https://en.energinet.dk/Energy-data/Guarantees-of-origin-el-gas-hydrogen/) for proving the origin of electricity. 


## What Is Project-Origin?

Project-Origin is an Open Source project that is focused on creating verifiable and unique objects for the [Energy Track and Trace](https://energytrackandtrace.com/), Granular Certification Scheme. There are two main features of Project-Origin:

1. Merkle Tree implementation - ensures that the registry is tamper-evident, auditible and that the individual entries are unique and verifiable using proof of inclusion.
2. Pedersen Commitment implementation - ensures that the sensitive data is not stored in the registry, but only a commitment to the data. This ensures that the data is not leaked, but can be verified by the registry. The commitments are non-retrievable, and can hence never be retrieved by external parties or future systems such as quantum computers.

Project-Origin functions as a layer-2 for logs or for external blockchains to leverage the trust from issuing bodies to 3. party service providers and consumers by ensuring that the data is verifiable and unique and not possible to tamper with from registry operators after publication on data-bearing blockchain. The functionality is a pre-requisite for a decentralized energy market, where the actual data is stored on conventional infrastructure, but the merkle-tree hashes that can contain large amounts o transactions in a single hash from the certificate transactions on a registry in a form that is verifiable and unique.

In order to facilitate a **[Federated](https://arxiv.org/pdf/1202.4503.pdf) Infrastructure** it is of utmost importance to have trust towards the other parties involved in the infrastructure. Hence the Registry can be audited in real-time and expose a health check of the entries in the Registry to other registries and that audits can be done by independent 3. parties as well without revealing sensitive information. This is especially true for the energy sector, where the infrastructure is highly regulated and the data is sensitive. 

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

## Need any help? 
In the registry repository, the Trusted Committer is currently @wisbech. The Trusted Committer provides timely support and mentorship for contributors, and helps contributors to shape their pull request to be ready to be submitted/accepted. If you want to contribute to this repository, the Trusted Committer has the mandate to specify any requirements that the contribution must fulfill on behalf of the partnership, to ensure product quality. 

## Documentation
To learn more about the implementation [go to the complete documention.](https://project-origin.github.io/registry/)
