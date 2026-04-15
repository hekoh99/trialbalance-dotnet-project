using TriBalance.Application.Common.Messaging;

namespace TriBalance.Application.Validation.Commands.TriggerValidation;

public record TriggerValidationCommand(Guid EngagementId, Guid TrialBalanceId)
    : ICommand<ValidationJobDto>;
