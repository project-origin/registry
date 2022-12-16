using System.Numerics;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Models;

internal record Slice(Commitment Source, Commitment Quantity, Commitment Remainder, BigInteger ZeroR);
