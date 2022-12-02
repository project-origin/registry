# Postgres Event Store


## Tables

### Batches

Holds information about the batches.

A batch has 4 states:

* New = 1
* Full = 2
* Publishing = 3
* Finalized = 4

### Streams

Contains information about a stream like id and current version.

### Events

Contains the persisted event. Has references to streams and batches.
