using TriBalance.Domain.Engagement;

namespace TriBalance.Application.Engagements;

internal static class EngagementMapper
{
    public static EngagementDto ToDto(Engagement e) => new(
        e.Id,
        e.ClientName,
        e.FiscalYearEnd,
        e.CreatedAt,
        e.TrialBalances.Select(ToSummary).ToList());

    public static TrialBalanceSummaryDto ToSummary(TrialBalance tb) => new(
        tb.Id,
        tb.FileName,
        tb.SubmittedAt,
        tb.TotalDebits,
        tb.TotalCredits,
        tb.IsBalanced,
        tb.GlEntries.Count);
}
