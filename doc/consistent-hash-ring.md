# Consistent hash ring

It is important when processing transactions that all transactions for a given stream are processed sequentially, so no two transactions for the same stream are processed concurrently.
The registry service uses a consistent hash ring to distribute transactions into seperate queues based on the stream id. This ensures that all transactions for a given stream are processed sequentially.

More about consistent hash rings can be found [here](https://www.toptal.com/big-data/consistent-hashing).

Using consistent hash rings also allows us to scale the registry service up and down, with a minimum of transactions having to be rescheduled into other queues.

## Rescheduling transactions

When processors are added or removed from the hash ring, transactions may need to be rescheduled into other queues.
Each processor is responsible for rescheduling transactions from its queue into other queues. This is done by checking if the transaction's stream id is still part of the hash ring.

## Queue cleanup

The queue cleanup service continuously checks the broker for queues that are no longer part of the hash ring (if servers or threads have been reduced), reschedules the transactions in the queue to other queues, and deletes the queue.

## Configuration

The number of queues in the hash ring is a product of the number of replicas of the container and the number of threads per container.

```yaml
# transactionProcessor holds the configuration for the transaction processor
transactionProcessor:
  # replicas defines the number of transaction processor containers to run
  replicas: 3
  # threads defines the number of parallel threads to run per transaction processor
  threads: 5
  # defines the weight in the consistent hash ring used to determine which transaction processor to send a transaction to.
  weight: 10
```

The weight is used to determine the number posistions each queue takes up in the hash ring. This is to better distribute transactions between queues.
