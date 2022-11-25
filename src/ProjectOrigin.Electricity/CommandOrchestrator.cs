using Google.Protobuf;
using ProjectOrigin.Electricity.Consumption;
using ProjectOrigin.Electricity.Production;
using ProjectOrigin.Electricity.V1;
using ProjectOrigin.Register.CommandProcessor.Interfaces;
using ProjectOrigin.Register.CommandProcessor.Models;
using ProjectOrigin.Register.StepProcessor.Interfaces;
using ProjectOrigin.Register.StepProcessor.Models;

namespace ProjectOrigin.Electricity;

public class CommandOrchestrator : ICommandOrchestrator<IssueConsumptionCommand>, ICommandOrchestrator<IssueProductionCommand>, ICommandOrchestrator<ClaimCommand>, ICommandOrchestrator<TransferProductionSliceCommand>
{
    private ICommandStepProcessor _stepProcessor;

    public CommandOrchestrator(ICommandStepProcessor stepProcessor)
    {
        _stepProcessor = stepProcessor;
    }

    public async Task<Register.V1.CommandStatus> Process(Command<IssueConsumptionCommand> command)
    {
        var step = new CommandStep<IssueConsumptionCommand.Types.ConsumptionIssuedEvent>(
            command.Content.Event.CertificateId.ToModel(),
            new SignedEvent<IssueConsumptionCommand.Types.ConsumptionIssuedEvent>(command.Content.Event, command.Content.Signature.ToByteArray()),
            typeof(ConsumptionCertificate),
            command.Content.Proof);

        var result = await _stepProcessor.Process(step);

        return ToCommandStatus(command, result);
    }

    public async Task<Register.V1.CommandStatus> Process(Command<IssueProductionCommand> command)
    {
        var step = new CommandStep<IssueProductionCommand.Types.ProductionIssuedEvent>(
            command.Content.Event.CertificateId.ToModel(),
            new SignedEvent<IssueProductionCommand.Types.ProductionIssuedEvent>(command.Content.Event, command.Content.Signature.ToByteArray()),
            typeof(ProductionCertificate),
            command.Content.Proof);

        var result = await _stepProcessor.Process(step);

        return ToCommandStatus(command, result);
    }

    public async Task<Register.V1.CommandStatus> Process(Command<TransferProductionSliceCommand> command)
    {
        var step = new CommandStep<TransferProductionSliceCommand.Types.ProductionSliceTransferredEvent>(
            command.Content.Event.CertificateId.ToModel(),
            new SignedEvent<TransferProductionSliceCommand.Types.ProductionSliceTransferredEvent>(command.Content.Event, command.Content.Signature.ToByteArray()),
            typeof(ProductionCertificate),
            command.Content.Proof);

        var result = await _stepProcessor.Process(step);

        return ToCommandStatus(command, result);
    }

    public async Task<Register.V1.CommandStatus> Process(Command<ClaimCommand> command)
    {

        var result = await _stepProcessor.Process(
            new CommandStep<ClaimCommand.Types.AllocatedEvent>(
                command.Content.ProductionAllocatedEvent.ProductionCertificateId.ToModel(),
                new SignedEvent<ClaimCommand.Types.AllocatedEvent>(command.Content.ProductionAllocatedEvent, command.Content.ProductionAllocatedSignature.ToByteArray()),
                typeof(ProductionCertificate),
                command.Content.ProductionAllocatedProof));

        if (result.State != CommandStepState.Completed)
            return ToCommandStatus(command, result);

        result = await _stepProcessor.Process(
            new CommandStep<ClaimCommand.Types.AllocatedEvent>(
                command.Content.ConsumptionAllocatedEvent.ConsumptionCertificateId.ToModel(),
                new SignedEvent<ClaimCommand.Types.AllocatedEvent>(command.Content.ConsumptionAllocatedEvent, command.Content.ConsumptionAllocatedSignature.ToByteArray()),
                typeof(ConsumptionCertificate),
                command.Content.ConsumptionAllocatedProof));

        if (result.State != CommandStepState.Completed)
            return ToCommandStatus(command, result);

        result = await _stepProcessor.Process(
            new CommandStep<ClaimCommand.Types.ClaimedEvent>(
                command.Content.ProductionClaimedEvent.CertificateId.ToModel(),
                new SignedEvent<ClaimCommand.Types.ClaimedEvent>(command.Content.ProductionClaimedEvent, command.Content.ProductionClaimedSignature.ToByteArray()),
                typeof(ProductionCertificate)));

        if (result.State != CommandStepState.Completed)
            return ToCommandStatus(command, result);

        result = await _stepProcessor.Process(
            new CommandStep<ClaimCommand.Types.ClaimedEvent>(
                command.Content.ConsumptionClaimedEvent.CertificateId.ToModel(),
                new SignedEvent<ClaimCommand.Types.ClaimedEvent>(command.Content.ConsumptionClaimedEvent, command.Content.ConsumptionClaimedSignature.ToByteArray()),
                typeof(ConsumptionCertificate)));

        return ToCommandStatus(command, result);
    }

    private static Register.V1.CommandStatus ToCommandStatus(Command command, CommandStepResult result) =>
        new Register.V1.CommandStatus()
        {
            Id = ByteString.CopyFrom(command.Id),
            State = (result.State == CommandStepState.Completed) ? Register.V1.CommandState.Succeeded : Register.V1.CommandState.Failed,
            Error = result.ErrorMessage ?? string.Empty
        };
}
