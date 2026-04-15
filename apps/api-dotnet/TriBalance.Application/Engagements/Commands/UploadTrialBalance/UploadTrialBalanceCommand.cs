using TriBalance.Application.Common.Messaging;

namespace TriBalance.Application.Engagements.Commands.UploadTrialBalance;

public record UploadTrialBalanceCommand(
    Guid EngagementId,
    string FileName,
    Stream FileStream) : ICommand<TrialBalanceSummaryDto>;
