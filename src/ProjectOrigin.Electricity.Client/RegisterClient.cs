using System.Security.Cryptography;
using Google.Protobuf;
using Grpc.Net.Client;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client.Models;

namespace ProjectOrigin.Electricity.Client;

/// <summary>
/// Abstract RegisterClient that can be extended to enable specific functionality required.
/// </summary>
public class RegisterClient : IDisposable
{
    private GrpcChannel _channel;
    private Register.V1.CommandService.CommandServiceClient _grpcClient;

    /// <summary>
    /// Enables one to subscribe to CommandStatusEvent for commands one has initiated.
    /// </summary>
    public event Action<CommandStatusEvent>? Events;

    /// <summary>
    /// Create a RegisterClient based on an existing GrpcChannel.
    /// </summary>
    /// <param name="channel">the channel to use.</param>
    public RegisterClient(GrpcChannel channel)
    {
        _channel = channel;
        _grpcClient = new Register.V1.CommandService.CommandServiceClient(channel);
    }

    /// <summary>
    /// Create a RegisterClient based on a string url for the address of the gRPC endpoint for the registry.
    /// </summary>
    /// <param name="registryAddress">the url with the address to the registry.</param>
    public RegisterClient(string registryAddress)
    {
        _channel = GrpcChannel.ForAddress(registryAddress);
        _grpcClient = new Register.V1.CommandService.CommandServiceClient(_channel);
    }

    /// <summary>
    /// Disposes the RegisterClient and the <b>GrpcChannel</b>,
    /// beware if constructed with channel that is used elsewhere.
    /// </summary>
    public void Dispose() => _channel.Dispose();

    internal static ByteString Sign(Key signerKey, IMessage e)
    {
        return ByteString.CopyFrom(NSec.Cryptography.Ed25519.Ed25519.Sign(signerKey, e.ToByteArray()));
    }

    internal void TriggerEvent(CommandStatusEvent status)
    {
        if (Events != null)
        {
            var delegates = Events.GetInvocationList();
            Parallel.ForEach(delegates, d => d.DynamicInvoke(status));
        }
    }

    internal Task<CommandId> Execute(Register.V1.Command command)
    {
        Task.Run(() => TaskExecute(command));

        return Task.FromResult(new CommandId(command.Id.ToByteArray()));
    }

    internal async Task TaskExecute(Register.V1.Command command)
    {
        var id = new CommandId(command.Id.ToByteArray());

        try
        {
            var result = await _grpcClient.SubmitCommandAsync(command);
            TriggerEvent(new CommandStatusEvent(id, (CommandState)(int)result.State, result.Steps.SingleOrDefault(x => x.State == Register.V1.CommandState.Failed)?.Error));
        }
        catch (Exception ex)
        {
            TriggerEvent(new CommandStatusEvent(id, CommandState.Failed, ex.Message));
        }
    }

    internal Register.V1.CommandStep CreateAndSignCommandStep(Key issuingBodySigner, Register.V1.FederatedStreamId federatedId, IMessage @event) =>
            new Register.V1.CommandStep()
            {
                RoutingId = federatedId,
                SignedEvent = new Register.V1.SignedEvent()
                {
                    Type = V1.ConsumptionIssuedEvent.Descriptor.FullName,
                    Payload = @event.ToByteString(),
                    Signature = Sign(issuingBodySigner, @event)
                }
            };
}
