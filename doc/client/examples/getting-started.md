# Getting Started

This getting started expects a running registry you can connect to.

To call the registries, one should use the [client library available on nuget.org](https://www.nuget.org/packages/ProjectOrigin.Electricity.Client/).

## Connecting to a Registry

All commands to a registry happens through a RegisterClient,
this can be instanciated with the help of the address of where
the registry is located.

```csharp
var client = new RegisterClient("http://my-registry")
```

## Issuing your first certificate

To issue a certificate, one has to use the ElectricityCommandBuilder.

When executed, it only returns a reference to the command,
to get the result one has to listen to [async responses](#events)

```csharp
RegisterClient client;
Key issuerKey;

var gsrn = new ShieldedValue(57000001234567);
var quantity = new ShieldedValue(150);
var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

var commandBuilder = new ElectricityCommandBuilder();

commandBuilder.IssueConsumptionCertificate(
    new FederatedCertifcateId(
        "RegistryA",    // The identifier for the registry
        Guid.NewGuid()  // The unique id of the certificate, this should be saved.
    ),
    new Client.Models.DateInterval(
        new DateTimeOffset(2022, 10, 1, 12, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2022, 10, 1, 13, 0, 0, TimeSpan.Zero)
    ),
    "DK1", // The area of the
    gsrn,
    quantity,
    ownerKey.PublicKey,
    issuerKey
    );

var commandId = await commandBuilder.Execute(client);

```

## Events

Reponses are send async back to clients,
they can be listened to by attaching to the client.Events handle.

```csharp
var client = new RegisterClient("http://my-registry")

client.Events + (event) => {
    Console.WriteLine(event.Id);
    Console.WriteLine(event.State);
}
```
