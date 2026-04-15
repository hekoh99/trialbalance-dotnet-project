using TriBalance.Application.Common.Messaging;

namespace TriBalance.Application.Engagements.Commands.CreateEngagement;

public record CreateEngagementCommand(string ClientName, DateTime FiscalYearEnd)
    : ICommand<EngagementDto>;
