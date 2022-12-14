# ProjectOrigin

## What Is ProjectOrigin?

ProjectOrigin is a OpenSource project to create a **Federated Registry**
to handle [**Granular Certificates**](concept/granular-certificates/readme.md)
to prove the origin and conversions of Energy in the Green transition.

## Why ProjectOrigin?

ProjectOrigin was created because there is a need to provide a
**public verifiable** way to prove the origin of the electricity one uses on
with a high granularity.

If one searches for greenwashing there is no shortages of articles on the
internet showing the growing scepticism with the current system.

## How it works

To make the data **publicly verifiable**, it is required for data
to a place were everyone can read the data.

To solve the apparent privacy issues[^1] this would create,
the data is encrypted with the help of [Pedersen Commitments](concept/pedersen-commitments.md),
which cannot be decrypted, but only proven.

[^1]: Exposings users measurement data publically could give insights into companies and citizens
usage, which is very undesireable and potentially damaging.


To solve the scalability issues[^2] we chose to go with a **federated setup**
instead of a fully distributed setup directly on distributed ledger.

This is implemented as a layer-2 blockchain, where batches of data for
each registry is hashed in a merkle-tree, and each root is written to a ledger.

This ensures immutability, while providing high throughput.

[^2]: In Denmark alone we have 3.500.000 electricity meters,
and with hourly measurements this would create
30.660.000.000 (30 billion) measurements yearly.

More in depth information can be found in the [architecture description](architecture/overview.md).

## Work in progress

The documentation and everything else is a work in progress,
so mind that everything might not be well documented,
but please [create an issue](https://github.com/project-origin/registry/issues/new/choose)
with what aspects we should document further.

## Development Status

Progress can be [tracked in the roadmap](roadmap.md).
