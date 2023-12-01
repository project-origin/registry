using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.IdentityModel.Tokens;
using ProjectOrigin.Common.V1;
using ProjectOrigin.Electricity.Example.Extensions;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.PedersenCommitment;
using RegistryV1 = ProjectOrigin.Registry.V1;
using WalletV1 = ProjectOrigin.WalletSystem.V1;

namespace ProjectOrigin.Electricity.Example;

public class WithWalletFlow
{
    public required string Area { get; init; }
    public required string IssuerKey { get; init; }
    public required string ProdRegistryName { get; init; }
    public required string ProdRegistryAddress { get; init; }
    public required string ConsRegistryName { get; init; }
    public required string ConsRegistryAddress { get; init; }
    public required string WalletAddress { get; init; }

    public async Task<int> Run()
    {
        // Decode text key from base64 string
        var keyText = Encoding.UTF8.GetString(Convert.FromBase64String(IssuerKey));

        // Import the key for the issuing body of the area;
        var issuerKey = Algorithms.Ed25519.ImportPrivateKeyText(keyText);

        // Create a random subject for the wallet
        var subject = Guid.NewGuid().ToString();
        Console.WriteLine($"Using subject {subject}");

        // Create channel and client
        var prodChannel = GrpcChannel.ForAddress(ProdRegistryAddress);
        var prodClient = new RegistryV1.RegistryService.RegistryServiceClient(prodChannel);

        // Create channel and client
        var consChannel = GrpcChannel.ForAddress(ConsRegistryAddress);
        var consClient = new RegistryV1.RegistryService.RegistryServiceClient(consChannel);

        // Create channel and client
        var walletChannel = GrpcChannel.ForAddress(WalletAddress);
        var walletClient = new WalletV1.WalletService.WalletServiceClient(walletChannel);
        var receiveClient = new WalletV1.ReceiveSliceService.ReceiveSliceServiceClient(walletChannel);

        // Instanciate eventBuilder with registry and area info
        var eventBuilder = new ProtoEventBuilder
        {
            GridArea = Area,
            Start = new DateTimeOffset(2023, 1, 1, 12, 0, 0, 0, TimeSpan.Zero),
            End = new DateTimeOffset(2023, 1, 1, 13, 0, 0, 0, TimeSpan.Zero)
        };

        // ------------------  Create wallet deposit endpoint ------------------
        var header = GenerateHeader(subject);
        var depositEndpoint = await walletClient.CreateWalletDepositEndpointAsync(new WalletV1.CreateWalletDepositEndpointRequest(), header);
        var ownerKey = Algorithms.Secp256k1.ImportHDPublicKey(depositEndpoint.WalletDepositEndpoint.PublicKey.Span);
        var depositEndpointPosition = 0;

        // ------------------  Issue Consumption ------------------
        // Create a new ConsumptionIssuedEvent, sign it and sent it to the registry.
        {
            Console.WriteLine($"Issuing consumption Granular Certificate");

            var consCertId = eventBuilder.ToCertId(ConsRegistryName, Guid.NewGuid());
            var consCommitmentInfo = new SecretCommitmentInfo(250);

            // Set next deposit endpoint position
            depositEndpointPosition++;

            // Derive next Heriarchical Deterministic Key
            var haKey = ownerKey.Derive(depositEndpointPosition).GetPublicKey();

            // Create issued event
            var consumptionIssued = eventBuilder.CreateConsumptionIssuedEvent(consCertId, consCommitmentInfo, haKey);

            // Sign the event as a transaction
            var signedTransaction = issuerKey.SignTransaction(consumptionIssued.CertificateId, consumptionIssued);

            // Send transaction to registry, and wait for committed state
            await consClient.SendTransactionAndWait(signedTransaction);

            // Send information to wallet using the original publicKey and position
            await SendInfoToWallet(receiveClient, depositEndpointPosition, depositEndpoint.WalletDepositEndpoint.PublicKey, consCertId, consCommitmentInfo);
        }

        // ------------------  Issue production ------------------
        // Create a new ProductionIssuedEvent, sign it and sent it to the registry.
        {
            Console.WriteLine($"Issuing Production Granular Certificate");

            var prodCertId = eventBuilder.ToCertId(ProdRegistryName, Guid.NewGuid());
            var prodCommitmentInfo = new SecretCommitmentInfo(350);

            // Set next deposit endpoint position
            depositEndpointPosition++;

            // Derive next Heriarchical Deterministic Key
            var haKey = ownerKey.Derive(depositEndpointPosition).GetPublicKey();

            // Create issued event
            var productionIssued = eventBuilder.CreateProductionIssuedEvent(prodCertId, prodCommitmentInfo, haKey);

            // Sign the event as a transaction
            var signedTransaction = issuerKey.SignTransaction(productionIssued.CertificateId, productionIssued);

            // Send transaction to registry, and wait for committed state
            await prodClient.SendTransactionAndWait(signedTransaction);

            // Send information to wallet using the original publicKey and position
            await SendInfoToWallet(receiveClient, depositEndpointPosition, depositEndpoint.WalletDepositEndpoint.PublicKey, prodCertId, prodCommitmentInfo);
        }

        // ------------------  Query wallet ------------------
        {
            await Task.Delay(5000);
            Console.WriteLine($"Querying wallet");

            var walletInfo = await walletClient.QueryGranularCertificatesAsync(new WalletV1.QueryRequest(), header);

            Console.WriteLine($"Wallet certificates:");
            Console.WriteLine($"- Registry - StreamId - Type - Quantity");
            foreach (var cert in walletInfo.GranularCertificates)
            {
                Console.WriteLine($"- {cert.FederatedId.Registry} - {cert.FederatedId.StreamId.Value} - {cert.Type} - {cert.Quantity}");
            }
            Console.WriteLine($"---------------------------------");
        }

        return 0;
    }

    private static async Task SendInfoToWallet(WalletV1.ReceiveSliceService.ReceiveSliceServiceClient walletClient, int position, ByteString publicKey, FederatedStreamId certId, SecretCommitmentInfo consCommitmentInfo)
    {
        await walletClient.ReceiveSliceAsync(new WalletV1.ReceiveRequest
        {
            WalletDepositEndpointPosition = (uint)position,
            WalletDepositEndpointPublicKey = publicKey,
            CertificateId = certId,
            Quantity = consCommitmentInfo.Message,
            RandomR = ByteString.CopyFrom(consCommitmentInfo.BlindingValue)
        });
    }

    private static Metadata GenerateHeader(string subject)
    {
        var _ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var claims = new[]
        {
            new Claim("sub", subject),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        var key = new ECDsaSecurityKey(_ecdsa);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new Metadata
        {
            { "Authorization", $"Bearer {tokenString}" }
        };
    }
}
