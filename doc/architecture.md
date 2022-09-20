# Architecture

This file contains the run though of the suggested architecture for the registry.

## Container diagram

Below is a [Container diagram](https://c4model.com/#ContainerDiagram) showing a break down of the registry into
high level of what containers the system exists of.

![C4 Container diagram](/doc/container_diagram.drawio.svg)

For in depth description of the containers look in the following documents:

- [Layer 2 - Verifiable event store](/doc/layer2_verifiable_event_store/README.md)
- [Layer 3 - Logic](/doc/layer3_logic/README.md)
- [Layer 4 - Privacy](/doc/layer4_privacy/README.md)

## Glossary

- **Signed commands**: are a request to the registry to perform a state change, it is signed by the owner of the item to perform changes upon.
- **Transactions**: a series of actions to perform the signed command, which results in n number of events.
- **Actions**: are a request to perform a state change and create an event on a registry.
- **Events**: are state changes that has happened and are persisted when they have been included in a merkle tree.
- **Event store**: a datastore that stores all the events for the registry.
