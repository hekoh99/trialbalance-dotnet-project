using TriBalance.Application.Validation;

namespace TriBalance.Infrastructure.Persistence.CosmosDB;

/// <summary>
/// Fallback used when Cosmos DB is not configured. Returns null so /validation
/// responds 404 instead of crashing at DI resolution.
/// </summary>
public sealed class DisabledValidationResultRepository : IValidationResultReader
{
    public Task<ValidationResultDto?> GetLatestByEngagementAsync(
        Guid engagementId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ValidationResultDto?>(null);
}
