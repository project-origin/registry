# Project-Origin Pedersen Commitment

This client library has an interface and currently a single implementation of HD keys as descriped in BIP32, and implemented using NBitcoin with the Secp256k1 curve.

## How to

To create a Pedersen Commitment one must create a new instance of a SecretCommitmentInfo with the quantity (message : uint) one wants to hide.

```csharp

var secret = new SecretCommitmentInfo(250);

```

The secret has two fields that must be persisted to later prove the data, the Message and BlindingValue.

```csharp
// store these values
uint message = secret.Message;
ReadOnlySpan<byte> blinding = secret.BlindingValue;
```

The commitment to share publicly can be gotten directly from the SecretCommitmentInfo.

```csharp
ReadOnlySpan<byte> commitment = secret.Commitment.C;
```

### Range proofs

If one wants to prove the commitment is within the allowed value, one can easily create a range proof using a label:

> Note: In ProjectOrigin Electricity the label is always the string representation of certificate uuid.

```csharp
string someLabel = "foobar";
ReadOnlySpan<byte> rangeProof = secret.CreateRangeProof(someLabel);
```

The range proof is hardcoded to allow values between 0 and uint.Max (32 bits).

#### Why use a label?

The label is to ensure that the range proof is unique as to ensure that the same range proof is not used for multiple commitments.
This is important as the range proof is a zero-knowledge proof, and if the same range proof is used for multiple commitments, one could use the range proof for exploits.
