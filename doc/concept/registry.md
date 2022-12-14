# Registries

## What is a Registry?

In ProjectOrigin a registry is a single node in the federated network.

Each registry can hold any number of [GCs](./granular-certificates/readme.md)
it is up to the issuing body[^ib] to specify what registry a GC is on
at the time of issuance with the help of the [Federated Certificate ID](./granular-certificates/federated-certifate-id.md)

[^ib]: The issuing body is the entity that has the lawfull right to issued GCs within an area.

The life-cycle of a single GC always says within the same registry
as to not have to reach consensus between registries.

Some commands like [claim](./granular-certificates/commands/claim.md)
does span multiple registries, but are performed in a two-phase commit fasion.

## registry != Area

GCs for the same [area](./granular-certificates/attributes.md#grid-area)
can exists on different registries, aswell as GCs for multiple areas can
exists on the same registry.

The registry is simple put, a node in a network holding and ensuring the
rules of the network are applied, but there is no hard fast rule on how
these should be split.

## Federated network?

ProjectOrigin chose to go with a layer 2 blockchain approach where the data would be `federated`.
In practice this makes each registry the authority of what is the truth for the data (GCs)
held on that registry.

This removes the need for registries to reach consensus each time commands happen within a registry.
but requires `two-phase commits` when commands are performed that make changes on multiple streams (GCs).

These changes ensure that a high throughput can happen on the platform, but at the expense of latency,
when commands happen across multiple registries, in introduces a bit of latency.

## Layer 2, what is that?

All [commands and events](./command-and-events.md) is executed and stored within each registry,
but to ensure immutability, the nodes continually create batches of all events within a registry,
creates a merkle-tree and publishes the root to a blockchain[^concordium].
This ensures that no events can be changed after they have been committen in a batch,
and a user can get a merkle-proof, prove that the event exists within the batch.

[^concordium]: Concordium currently, but any blockchain could be used.

**Layer 2** refers to that all data happens not directly on a blockchain,
but on a layer build on top of a blockchain.
