namespace TriBalance.Application.Validation;

public record ValidationJobDto(
    Guid Id,
    Guid EngagementId,
    Guid TrialBalanceId,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt,
    string? ErrorMessage);
