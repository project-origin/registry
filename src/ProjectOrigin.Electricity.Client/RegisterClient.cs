using System.Security.Cryptography;
using Google.Protobuf;
using Grpc.Net.Client;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client.Models;

namespace ProjectOrigin.Electricity.Client;

/// <summary>
/// Abstract RegisterClient that can be extended to enable specific functionality required.
/// </summary>
public abstract class RegisterClient : IDisposable
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

    internal ByteString Sign(Key signerKey, IMessage e)
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

    internal Task<CommandId> SendCommand(IMessage commandContent)
    {
        var name = commandContent.GetType().FullName!;

        var command = new Register.V1.Command()
        {
            Type = name,
            Payload = commandContent.ToByteString(),
        };

        var commandHash = SHA256.HashData(command.ToByteArray());
        command.Id = ByteString.CopyFrom(commandHash);

        Task.Run(() => Execute(command));

        return Task.FromResult(new CommandId(commandHash));
    }

    internal async Task Execute(Register.V1.Command command)
    {
        var id = new CommandId(command.Id.ToByteArray());

        try
        {
            var result = await _grpcClient.SubmitCommandAsync(command);
            TriggerEvent(new CommandStatusEvent(id, (CommandState)(int)result.State, result.Error));
        }
        catch (Exception ex)
        {
            TriggerEvent(new CommandStatusEvent(id, CommandState.Failed, ex.Message));
        }
    }
}
