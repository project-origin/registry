---
uid: granular_certificate
---

# Granular Certificate

**Granular Certificate** is a term defined by the [EnergyTag Organization](https://energytag.org).

It is a goal at some point for the **ProjectOrigin Registry** to be **Compliant with the EnergyTag Standard**

## Description

In ProjectOrigin, a Granular Certificate (GC) can describe either a **production or consumption** of energy at a meter [^et].

[^et]: In the EnergyTag scheme a GC only relates to the production of energy,
    consumption verification is done with the help of a *consumption verification body*.

A GC is immutable, in that the data on a GC cannot be changed after it has been issued.

All commands and the life-cycle for a GC happens through its [slices](#Slices).

A GC describes a [quantity](#quantity) of energy, which is consumed or produced by a specific [meter](#gsrn), within a [grid area](#grid-area), and a set [period](#period),

More on the attributes of a certificate can be found in the [attributes section](attributes.md)

---

## Slices

As described above, a GC is immutable, and contains the entire quantity for the meter for the period,
***so how could trading and claiming parts of a certificate work?***

To solve this ProjectOrigin borrowed a term from the financial sector, refering to **stock-slicing**,
where a single stock can be traded as slices. This happens on top of the existing stock marked.

This was implemented as a core part of the architecture of the GC system in ProjectOrigin.
A GC consists of **1 to n slices**, when the GC is issued, **one initial slice** is created.
Similar to baking a cake, "1 slice (whole cake)" exists.

When a GC is issuied, it is created with an single initial slice.

A slice is always owned by a single public-key.[^public-key]

[^public-key]: Public-private keys was chosen since the registries do not have the concept of accounts and users. Ownership of a GC is purely done with the help of a public-private keypair.

### Slice life-cycle

A slice has a specific life-cycle, when the slice is created, it becomes **active**.

Note that most commands are final, in there is no way to reverse them once performed.

```mermaid
stateDiagram-v2
    [*] --> Active: Slice created
    Active --> Active: Transfer command
    Active --> Claimed: Claim command
    Active --> Expired: Expires automatically
    Active --> Removed: Slice command
    Removed: Removed*
    note right of Removed
        Removed slices are are <b>not</b>
        counted when calculating the total,
        since new slices representing the
        amount was created.
    end note
```

- [Transfer command](commands/transfer.md): Transfers the ownership of a slice to a new owner.
- [Slice command](commands/slice.md): Create new slices from an existing slice.
- [Claim command](commands/claim.md): Claim a production slice to a consumption slice of same quantity.
- [Expire](commands/expire.md): Expires old slices.
