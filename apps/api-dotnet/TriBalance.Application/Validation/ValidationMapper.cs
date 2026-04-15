using TriBalance.Domain.Validation;

namespace TriBalance.Application.Validation;

internal static class ValidationMapper
{
    public static ValidationJobDto ToDto(ValidationJob j) => new(
        j.Id,
        j.EngagementId,
        j.TrialBalanceId,
        j.Status.ToString(),
        j.CreatedAt,
        j.UpdatedAt,
        j.CompletedAt,
        j.ErrorMessage);
}
