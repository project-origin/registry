namespace ProjectOrigin.Register.LineProcessor.Models;

public record CommandStepResult(CommandStepId Id, CommandStepState State, string? ErrorMessage = null);
