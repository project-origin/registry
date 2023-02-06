# Full flow example

Below is an example wher two GCs are issued, sliced and claimed.

- Consumption of 150Wh
- Production of 250Wh

The production is then sliced to a 150Wh slice and a remainder slice (100Wh).

A claim is then created between the two slices, and the commands are executed.

The commands could all have been executed independently, this is just an example.

```csharp
var commandBuilder = new ElectricityCommandBuilder();

var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
var period = new Client.Models.DateInterval(
            new DateTimeOffset(2022, 10, 1, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2022, 10, 1, 13, 0, 0, TimeSpan.Zero)
        );

// Add IssueConsumptionCertificate Command

var consCertId = new FederatedCertifcateId(
    Registries.RegistryB,
    Guid.NewGuid()
);
var consQuantity = new ShieldedValue(150);

commandBuilder
    .IssueConsumptionCertificate(
        consCertId,
        period,
        "DK1",
        new ShieldedValue(570000000009999),
        consQuantity,
        ownerKey.PublicKey,
        _dk1_issuer_key
    );

// Add IssueProductionCertificate Command

var prodCertId = new FederatedCertifcateId(
    Registries.RegistryB,
    Guid.NewGuid()
);
var prodQuantity = new ShieldedValue(250);

commandBuilder
    .IssueProductionCertificate(
        prodCertId,
        period,
        "DK1",
        "F01050100",
        "T020002",
        new ShieldedValue(570000000001213),
        prodQuantity,
        ownerKey.PublicKey,
        _dk1_issuer_key
    );

// Create slice of 150 on the ProdCert

var slicer = new Slicer(prodQuantity);
var prod_slice = new ShieldedValue(150);
slicer.CreateSlice(prod_slice, ownerKey.PublicKey);
var sliceCollection = slicer.Collect();
// remember to save the remainder - sliceCollection.Remainder

commandBuilder
    .SliceCertificate(
        prodCertId,
        sliceCollection,
        ownerKey
    );

// Create the claim
commandBuilder
    .ClaimCertificate(
        claimQuantity,
        consCertId,
        consQuantity,
        ownerKey,
        prodCertId,
        prod_slice,
        ownerKey
    );

// Last execute the list of commands.

var commandId = await commandBuilder.Execute(Client)
```
