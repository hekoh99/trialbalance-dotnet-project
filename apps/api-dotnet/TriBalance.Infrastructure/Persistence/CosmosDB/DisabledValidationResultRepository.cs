namespace TriBalance.Infrastructure.Persistence.CosmosDB;

/// <summary>
/// Fallback used when Cosmos DB is not configured. Returns null so
/// /validation responds 404 instead of crashing at DI resolution.
/// </summary>
public sealed class DisabledValidationResultRepository : IValidationResultRepository
{
    public Task<ClassificationResultDocument?> GetLatestByEngagementAsync(
        Guid engagementId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ClassificationResultDocument?>(null);
}
