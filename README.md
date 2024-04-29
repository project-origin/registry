# OriginRegistry

OpenSource project to create a registry to store and verify transactions for Granular Certificate schemes.

The name `OriginRegistry` references `Origin` which links the the projects name (Project-Origin),
and `Registry` as in a place to store and verify transactions.

For more information, see the [documentation](https://project-origin.github.io/docs/registry/index.html).

## tl:dr

The OriginRegistry is a federated system that verifies transactions for a given stream,
then stores the transaction in the stream in a tamper-evident way using a immutable log.

All streams in the federated system are identified by a `federated stream id`.

Doing it as a federated system ensures a high throughput since consensus is not needed for each transaction.

Transactions referencing data on other registries are done in a choreography pattern,
where distributed "transactions" are done in a sequence to ensure that the data is consistent.
This does add latency to the system, while not compromising the integrity or throughput of the system.

## Problem

The current system for proving the origin of electricity is not granular enough to support the green transition and power-to-X (PtX).

If one searches for greenwashing, there is no shortages of articles on the internet showing a growing scepticism with [the current system](https://en.energinet.dk/Energy-data/Guarantees-of-origin-el-gas-hydrogen/) for proving the origin of electricity.

Going to hourly or 15 minute intervals for the 300 million electricity meters in Europe would result in upwards of 1.2 billion transactions per hour,
or 11 trillion transactions per year.

Using blockchains for this would be infeasible as no blockchain can handle this amount of transactions.

Companies continuously strive to be more sustainable and to be able to prove that they are sustainable
in a way that is verifiable and trustworthy.

## Forces

OriginRegistry is designed to be a federated system to ensure high throughput and autonomy for the participants.

OriginRegistry should be able to handle a large amount of transactions, and the transactions should be verifiable and tamper-evident.

OriginRegistry should not hold any sensitive data but only a way to verify the data.

## Solution

The OriginRegistry is a federated system that verifies transactions for a given stream,
then stores the transaction in the stream in a tamper-evident way using a immutable log.

The data within a stream is never moved from a registry as to ensure that the data is consistent.

All streams in the federated system are identified by a `federated stream id`.
This id is unique for each stream and is used to reference the stream in transactions.

A registry uses a `verifier` to verify transactions for a given stream based on its current state.

## Energy Track and Trace

In [Energy Track and Trace](https://energytrackandtrace.azurewebsites.net), the `federated stream id` is the id of the Granular Certificate (GC),
and the `verifier` is the [electricity verifier](https://github.com/project-origin/verifier_electricity).

## Sketch

Below is a C4 system diagram of an overview of the system landscape OriginRegistry is a part of.

![C4 system diagram of OriginRegistry](./doc/system_diagram.drawio.svg)

## Resulting Context

The OriginRegistry enables high throughput of transactions for a given stream, which are guaranteed to be sequential.

All transactions are verifiable and tamper-evident, and the system continuously writes to a immutable log to ensure no data can be tampered with.
