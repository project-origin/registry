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

## Usage

### Store events

In order to store an event `store`should be called. When this method is called multiple things happens:

First we get the current `batch` - that is an `batch` with the state `1`. From that `batch` we need the `id` and number of `events`.

If we don´t have a batch we create one and inserts it into `batches`.

Next we get the current `index` for the `stream` that the `event` belongs to. Again, if we don´t have a `stream` from the `event` we will create one and insert it into `streams`.

We will now perform a optimistic concurrency check where we ensures that the `index` of the `event` is in the right order. If the check fails an `OutOfOrderException` will be thrown. If the check does not fail the `event` will be inserted into `events`.

We will now update the `batch` with the number of events and what state it´s in - if we have reached the point where the batch is full we will change state to `2`.

Finally we will update the stream with the next expected `index`.

### Finalizing batches
