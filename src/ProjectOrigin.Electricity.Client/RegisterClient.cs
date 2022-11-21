using Google.Protobuf;
using Grpc.Net.Client;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client.Models;
using ProjectOrigin.PedersenCommitment;

namespace ProjectOrigin.Electricity.Client;

public abstract class RegisterClient : IDisposable
{
    private GrpcChannel channel;
    private Register.V1.CommandService.CommandServiceClient grpcClient;

    public event Action<CommandStatusEvent>? Events;

    public Group Group { get => Group.Create(); }

    public RegisterClient(string registryAddress)
    {
        channel = GrpcChannel.ForAddress(registryAddress);
        grpcClient = new Register.V1.CommandService.CommandServiceClient(channel);
    }

    public void Dispose() => channel.Dispose();

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
            Version = 1,
            Payload = commandContent.ToByteString(),
        };

        var commandHash = command.ToByteArray();
        command.Id = ByteString.CopyFrom(commandHash);

        Task.Run(() => Execute(command));

        return Task.FromResult(new TransactionId(commandHash));
    }

    internal async Task Execute(Register.V1.Command command)
    {
        var id = new TransactionId(command.Id.ToByteArray());

        try
        {
            var result = await grpcClient.SubmitCommandAsync(command);
            TriggerEvent(new CommandStatusEvent(id, (CommandState)(int)result.State, result.Error));
        }
        catch (Exception ex)
        {
            TriggerEvent(new CommandStatusEvent(id, CommandState.Failed, ex.Message));
        }
    }
}
