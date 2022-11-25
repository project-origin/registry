namespace ProjectOrigin.Register.StepProcessor.Models;

public record CommandStepResult(CommandStepId Id, CommandStepState State, string? ErrorMessage = null);
