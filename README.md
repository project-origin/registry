# Registry

This repository contains the documentation and implementation of a first
release of the Registry from Project-Origin.

## Goal

The goal of [this project](https://github.com/orgs/project-origin/projects/1) is to create the first version of a **Federated Publicly Verifiable Registry**

The registry will enable the following:

- [ ] Data in the registry is publicly verifiable.
- [ ] Enable the issuance of Granular Certificates.
- [ ] Privacy of the individual quantities is hidden behind a Pedersen-commitments.
- [ ] Enable the claim of Granular Certificates to some consumption.
- [ ] Users can verify that they have a provable quantity of a Granular Certificate, that mathematically proves it is only used once.
- [ ] Federated setup enabling multiple registries to create a shared truth.
- [ ] The registry handles ownership based on public-private key-pairs.
- [ ] Users can transfer ownership of a sub-part of a certificate to another private-key.

## Architecture

Below is a [system context diagram](https://c4model.com/#SystemContextDiagram) showing what systems the registry integrates with.

![C4 system diagram](/doc/system_diagram.drawio.svg)

More in depth information can be found in the [architecture description](doc/architecture.md).
