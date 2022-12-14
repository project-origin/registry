---
uid: ProjectOrigin.Electricity.Client.Slicer
---

### Example

Below is an example of how to use the slicer.

```csharp
// In this example we create the ShieldedValue here,
// but it save from when it was issued.
PublicKey currentOwner, secondOwner, thirdOwner;
ShieldedValue sourceSlice = new ShieldedValue(500);

// Create the slicer
var slicer = new Slicer(sourceSlice);

// Create a shielded value and the slice.
var slice1 = new ShieldedValue(150);
slicer.CreateSlice(slice1, secondOwner);

// Create the second shielded value and the slice.
var slice2 = new ShieldedValue(100);
slicer.CreateSlice(slice2, thirdOwner);

// Collect all the slices to a collection.
var sliceCollection = slicer.Collect();

// The remainder will hold the shielded value for the remaining 250
ShieldedValue remainder = sliceCollection.Remainder;

// The sliceCollection is now ready to be used in a SliceCommand.
```
