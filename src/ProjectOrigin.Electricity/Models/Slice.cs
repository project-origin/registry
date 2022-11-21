using System.Numerics;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Models;

public record Slice(Commitment Source, Commitment Quantity, Commitment Remainder, BigInteger ZeroR);
