namespace TriBalance.Application.Engagements.Commands;

public record TriggerValidationCommand(Guid EngagementId, Guid TrialBalanceId);
