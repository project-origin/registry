using System.Security.Cryptography;
using Google.Protobuf;
using Grpc.Net.Client;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client.Models;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Client;

public abstract class RegisterClient : IDisposable
{
    private GrpcChannel _channel;
    private Register.V1.CommandService.CommandServiceClient _grpcClient;

    public event Action<CommandStatusEvent>? Events;

    public Group Group { get => Group.Default; }

    public RegisterClient(GrpcChannel channel)
    {
        _channel = channel;
        _grpcClient = new Register.V1.CommandService.CommandServiceClient(channel);
    }

    public RegisterClient(string registryAddress)
    {
        _channel = GrpcChannel.ForAddress(registryAddress);
        _grpcClient = new Register.V1.CommandService.CommandServiceClient(_channel);
    }

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

    internal Task<TransactionId> SendCommand(IMessage commandContent)
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

        return Task.FromResult(new TransactionId(commandHash));
    }

    internal async Task Execute(Register.V1.Command command)
    {
        var id = new TransactionId(command.Id.ToByteArray());

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
