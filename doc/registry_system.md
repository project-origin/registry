# Container diagram

Below is a [Container diagram](https://c4model.com/#ContainerDiagram)
showing a break down of the registry into high level containers as-is.

![C4 Container diagram](container.drawio.svg)

The system contains of the following containers:
- [Registry Service](./registry_service.md): The main container that contains the core logic of the registry.
- Message Broker: Contains the queues where transactions are stored before they are processed.
- Cache: Contains a cache of new and updated transactions status, to reduce the load on the database.
- Datastore: Contains valid transactions, blocks and publications.
- Verifier: Contains the logic to verify transactions, these are context aware, currently only one for electricty (Energy Track & Trace) is implemented.
