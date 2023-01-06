# Architecture

This file contains a run-through of the suggested architecture for the registry.

## Glossary

- **Commands**: are requests to the registry to perform a state change. A command is signed by the owner of the item to perform the change.
- **Events**: are state changes that has happened and are persisted when they have been included in a merkle tree.
- **Event store**: a datastore that stores all the events for the registry.

## Overview

Below is a [system context diagram](https://c4model.com/#SystemContextDiagram)
showing the landscape of systems the registry interacts with.

![C4 system diagram](system_diagram.drawio.svg)
