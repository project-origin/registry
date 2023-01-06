# Commands and events

Everything on the registries happen through [commands](./granular-certificates/commands/issue.md)

`Commands` are a request to the network to perform a given action.
They must be signed by the right entity[^auth].

[^auth]: The entity might either be an issuing body, or the owner of a GC.

`Events` are state changes within the network,
it is the result of a command.
If an [issue certificate](./granular-certificates/commands/issue.md) has been performed,
then a resulting CertificateIssued event has happened.

## Verifiable events

All events within a [registry](registry.md) are continually batched
with the help of a merkle-tree and published to a blockchain to ensure
immutability.

More to come...
