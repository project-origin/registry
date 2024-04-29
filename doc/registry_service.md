# Registry Service

Below is the C4 Container diagram for the Registry Service.

It contains the following components:

- `Registry Service` - Receives and responds to gRPC requests from clients, and queues transactions for processing in the broker.
- `QueueCleanupService` - Continuously checks the broker for queues that are no longer part of the consistent hash ring and deletes them.
- `Transaction Processor`- Processes transactions from the broker using a verifer, and persists valid transactions to the database. Updates the cache with transactions statuses.
- `Block finalizer` - Finalizes blocks by publishing them to a immutable log and updating the cache that transactions are finalized.

![C4 Container diagram](./diagrams/registry_service.component.drawio.svg)
