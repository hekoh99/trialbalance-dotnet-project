using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace TriBalance.Infrastructure.Persistence.CosmosDB;

public interface IValidationResultRepository
{
    /// <summary>
    /// Returns the most recent classification document for an engagement, or null.
    /// </summary>
    Task<ClassificationResultDocument?> GetLatestByEngagementAsync(
        Guid engagementId,
        CancellationToken cancellationToken = default);
}

public sealed class CosmosValidationResultRepository : IValidationResultRepository
{
    private readonly Container _container;
    private readonly ILogger<CosmosValidationResultRepository> _logger;

    public CosmosValidationResultRepository(
        CosmosClient client,
        CosmosOptions options,
        ILogger<CosmosValidationResultRepository> logger)
    {
        _container = client.GetContainer(options.DatabaseName, options.ClassificationContainer);
        _logger = logger;
    }

    public async Task<ClassificationResultDocument?> GetLatestByEngagementAsync(
        Guid engagementId,
        CancellationToken cancellationToken = default)
    {
        // Query within a single partition (engagementId) — cheap and ordered by processedAt desc.
        var query = new QueryDefinition(
            "SELECT TOP 1 * FROM c WHERE c.engagementId = @eid ORDER BY c.processedAt DESC")
            .WithParameter("@eid", engagementId.ToString());

        var iterator = _container.GetItemQueryIterator<ClassificationResultDocument>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(engagementId.ToString()),
                MaxItemCount = 1,
            });

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            foreach (var doc in response)
                return doc;
        }

        _logger.LogInformation("No classification result found for engagement {EngagementId}", engagementId);
        return null;
    }
}
