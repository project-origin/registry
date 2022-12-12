# ZKProof Specification

## Granular Certificates
This note describes how zero-knowledge proofs can be used to ensure that granular certificates contain correct values of energy.

A certificate may contain several values of energy (e.g., total, claimed and remainder). All energy amounts are included in the certificate in the form of a commitment to the energy amount. Two properties must usually be guaranteed:

1. The amount of energy is in a specific range - in particular non-negative
2. Sums of the energies add up currently (e.g., total energy equals the sum of claimed
and remaining energy).

Below we describe how zero-knowledge proofs can be used to ensure these properties so that they can be verified by anyone without knowing the actual values of energies in the certificate.

## Assumptions
### Algebras
Denote by `G` a prime-order group and let F be the (scalar) field of the same order. In the application, `G` will be a sub-group on an elliptic curve. The group operation in `G` (addition of two points on the curve) will be denoted `*`. We denote by `g^-1` the inverse of `g`.

### Commitments

We consider Pedersen commitments with the commitment `key (g,h) ∈ GxG`. This key must be generated such that no one (in particular not the prover) knows the pairwise discrete logarithm. This could be for example done by directly hashing digits of pi into G to get g and then hashing g to get h.

We denote by `C = Com(m;r) = g^m*h^r` the commitment to m with randomness/blinding.
Pedersen commitments are homomorphic, i.e., one can add up values within commitments. For example, let `C_a = C(a;r)` and `C_b = C(b;r’)`, then `C_a*(C_b)^-1` is a commitment to `a-b` with randomness `r-r’`.

#### Committed Values
In general each certificate contains the following commitments:
- `C = Com(m, r)` where `m` is the original value
- `C_i = Com(m_i;r_i)` where the `m_i` sum up to `m`

 We assume that the entity making the proof (the prover) knows all the values and all the randomness.

#### Proof
Public Input:
- The commitments in the certificate `C, C_1,...,C_n`
- The group `G`, commitment `key g,h`

Private Input:
- The values `m,m_1,...,m_n`
- Randomness `r,r_1,...,r_n`

Statement:

I know `m,m_1,...,m_n` and `r,r_1,...,r_n` such that:
- `C = Com(m;r)` and `C_i = Com(m_i;r_i)`
- `0<=m<=2^k` and `0<=m_i<=2^k`
- `m_1+..+m_n - m = 0`

Notes:

Previous we have shown the last claim be opening `(C_1*C_2*...C_n)*C^(-1)` as a commitment to `0`. As we now add zero-knowledge proofs we propose to do this with a zero-knowledge proof.
The parameter `k` must be determined. We expect for the moment that `k=20` allowing energy values from `0Wh` to more than `1 MWh` (this is also sufficient to ensure that `n2^k` is less than the order of `G` - preventing overflow in the addition).

#### Proof Implementation

The above proof can be implemented as a combination of an aggregated range proof and a sigma protocol for the zero-sum.

#### Range Proof
A simple way to implement range proofs is to use Bulletproofs.

Recommendation:
- We recommend using [Dalek Bulletproofs: A pure-Rust implementation of Bulletproofs using Ristretto](https://github.com/dalek-cryptography/bulletproofs) for these. Aggregation means that all ranges are proven within one proof.
- Use the `Ed25519` curve and `G` a subgroup of prime order.

#### Sum Proof
Consider commitment `C_sum = C_1*...*C_n*(C)^-1` which is a commitment to zero if `m_1+...+m_n - m = 0`.

In this case we have `C_sum = Com(0;r’) = h^{r’}` where `r’=r_1+...+r_n-r`.
  
Public Input:
- The commitment `C_sum`
- The group `G`, generator `h`

Private Input:
- Randomness `r’`

Statement:

I know `r’` such that `C_sum = h^{r’}`.

We therefore have reduced the sum proof to a simple proof of knowing a discrete logarithm.

Implementation:

This can be proven using a **Fiat-Shamir (FS)** transformed sigma protocol:

Prover:
1. Compute random `a <- F` and set `A = h^{a}` and input `A` into the **Fiat-Shamir** oracle.
2. Get challenge `c` from oracle - Standalone that is roughly `c = Hash(public inputs||A)`.
To make the proof more context dependent, one could feed more parts of the certificate into the oracle.
Also, a domain separation string, e.g. "EnergiNetCertificateProof", fed into the oracle would be good practice.
3. Compute `z = a-c*r’` and set `proof = (c,z)`

Verifier:
1. Compute `A=h^{z}*C_sum^c` and input `A` into the oracle.
2. Get the challenge from the oracle and accept if it equals `c`.

Combining Proofs

The proofs can be combined sequentially using the same oracle.

Prover:
1. Compute `C_sum`.
2. Input `g,h, C, C_1,...,C_n, C_sum` into the oracle. It would be reasonable to also include
a domain separation string, e.g. "EnergiNet Certificate Proof". If reuse between certificates is an issue one should also include other parts of the certificate, such as an ID.
3. Generate the range proof with the oracle
4. Generate the sigma proof with the oracle

Verifier:

1. Compute `C_sum`.
2. Input `g,h, C, C_1,...,C_n, C_sum` into the oracle
3. Verify the range proof with the oracle
4. Verify the sigma proof with the oracle


 
 