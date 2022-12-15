# Project-Origin Electricity Client

This client library enables communicating with a ProjectOrigin Registry V1, supporting the Electricity Commands V1.

Read more about how to use the library in the [documentation](https://project-origin.github.io/registry/api/ProjectOrigin.Electricity.Client.ElectricityClient.html).

## Example

Simple example on how to issue a Consumption Granular Certificate

```csharp
var client = new ElectricityClient("http://my-registry")
var dk1IssuerKey = Key.Create(SignatureAlgorithm.Ed25519);

//Remember to save to Shielded values, otherwise they cannot be proven.
var gsrn = new ShieldedValue(5700001234567);
var quantity = new ShieldedValue(150);
var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

var commandId = await Client.IssueConsumptionCertificate(
    new FederatedCertifcateId(
        "RegistryA",
        Guid.NewGuid()
    ),
    new Client.Models.DateInterval(
        new DateTimeOffset(2022, 10, 1, 12, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2022, 10, 1, 13, 0, 0, TimeSpan.Zero)
    ),
    "DK1",
    gsrn,
    quantity,
    ownerKey.PublicKey,
    dk1IssuerKey
    );
```
