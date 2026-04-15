namespace TriBalance.Application.Engagements;

/// <summary>
/// Application-layer read models. The API re-exposes these directly; if the
/// HTTP shape ever needs to diverge (e.g. field renames, hypermedia links),
/// Api can introduce its own response record and map.
/// </summary>
public record EngagementDto(
    Guid Id,
    string ClientName,
    DateTime FiscalYearEnd,
    DateTime CreatedAt,
    IReadOnlyList<TrialBalanceSummaryDto> TrialBalances);

public record TrialBalanceSummaryDto(
    Guid Id,
    string FileName,
    DateTime SubmittedAt,
    decimal TotalDebits,
    decimal TotalCredits,
    bool IsBalanced,
    int GlEntryCount);
