namespace TriBalance.Application.Validation;

/// <summary>
/// Outbound port for reading classification results persisted by the Python
/// Worker (Cosmos in production). Application defines the shape; Infrastructure
/// is responsible for mapping from the actual storage document.
/// </summary>
public interface IValidationResultReader
{
    Task<ValidationResultDto?> GetLatestByEngagementAsync(Guid engagementId, CancellationToken cancellationToken = default);
}

public record ValidationResultDto(
    string Id,
    Guid EngagementId,
    bool IsBalanced,
    decimal TotalDebits,
    decimal TotalCredits,
    decimal Variance,
    IReadOnlyList<ClassificationDto> Classifications,
    IReadOnlyDictionary<string, int> Summary,
    IReadOnlyList<IReadOnlyDictionary<string, object>> FlaggedItems,
    DateTime ProcessedAt);

public record ClassificationDto(
    string AccountCode,
    string AccountName,
    string ClassifiedAs,
    double Confidence,
    IReadOnlyList<IReadOnlyDictionary<string, object>> Flags,
    string? Reasoning);
