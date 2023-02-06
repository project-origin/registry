---
uid: federated_certificate_id
---

# Federated Certificate ID

The **Federated Certificate ID** (FID) is a combination of two parts:

- RegistryId: of the [registry](../registry.md) holding the GC[^1].
- StreamId: Is the unique id of the GC, it is a Uuid4.

[^1]: [Granular Certificate](readme.md)

This combined key is ALWAYS the full identifier for a GC,
even though the StreamID is unique,
the FID should always be used.

## Registry ID

The RegistryID is an identifier for the specific [registry](../registry.md)
holding the GC.

A GC's whole lifecycle always exists on a single registry,
removing the need for the federated network to reach consensus,
since the holding registry has the full mandate to invoke changes on the GC.

The RegistryID is used to route commands to the correct registry,
as to remove the need for a lookup table to identify which registry a
GC lives on.

## Stream ID

The StreamID is the unique id of the certificate.

The term StreamID comes from the underlying [verifiable event store](../../architecture/verifiable_event_store/README.md),
where all changes (events) on a GC is stored in an event stream for the GC.

The StreamID is a Uuid4, and is the unique identifier for a GC across the entire
federated network.
