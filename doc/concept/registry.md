# Registries

## What is a Registry?

In ProjectOrigin a registry is a single node in the federated network.

Each registry can hold any number of [GCs](./granular-certificates/readme.md).
It is up to the issuing body[^ib] to specify which registry to put a GC on
at the time of issuance with the help of the [Federated Certificate ID](./granular-certificates/federated-certifate-id.md)

[^ib]: The issuing body is the entity that has the legal right to issue GCs within a given area.

The life-cycle of a single GC always stays within the same registry
as to not have to reach consensus between registries.

Some commands like [claim](./granular-certificates/commands/claim.md)
does span multiple registries, but are performed in a two-phase commit fashion.

## registry != Area

GCs for the same [area](./granular-certificates/attributes.md#grid-area)
can exist on different registries, as well as GCs for multiple areas can
exists on the same registry.

The registry is, simply put, a node in a network holding and ensuring the
rules of the network are applied, but there is no hard rule on how
these should be split.

## Federated network?

Project-Origin chose to go with a _layer 2 blockchain_ approach where the data would be `federated`.
In practice this makes each registry the authority of what is the truth for the data (GCs)
held on that registry.

This removes the need for registries to reach consensus each time commands happen within a registry.
but requires `two-phase commits` when commands are performed that make changes on multiple streams (GCs).

These changes ensure that a high throughput can happen on the platform, but at the expense of latency,
when commands happen across multiple registries.

## Layer 2, what is that?

All [commands and events](./command-and-events.md) are executed and stored within each registry,
but to ensure immutability, the nodes continually create batches of all events within a registry,
creates a merkle-tree and publishes the root to a blockchain[^concordium].
This ensures that no events can be changed after they have been committed in a batch,
and a user can get a merkle-proof, to prove that the event exists within the batch.

[^concordium]: Concordium currently, but any blockchain could be used.

**Layer 2** refers to the abstraction of not pushing all data directly on a blockchain,
but on a layer built on top of a blockchain.
