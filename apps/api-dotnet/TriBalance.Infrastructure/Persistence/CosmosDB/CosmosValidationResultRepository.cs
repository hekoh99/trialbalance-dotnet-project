using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using TriBalance.Application.Validation;

namespace TriBalance.Infrastructure.Persistence.CosmosDB;

/// <summary>
/// Implements Application.Validation.IValidationResultReader on Cosmos DB.
/// Queries within a single engagementId partition for cheap, ordered reads.
/// Maps the Cosmos-shaped document to the Application DTO so upstream layers
/// don't depend on storage types.
/// </summary>
public sealed class CosmosValidationResultRepository : IValidationResultReader
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

    public async Task<ValidationResultDto?> GetLatestByEngagementAsync(
        Guid engagementId,
        CancellationToken cancellationToken = default)
    {
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
                return ToDto(doc, engagementId);
        }

        _logger.LogInformation("No classification result found for engagement {EngagementId}", engagementId);
        return null;
    }

    private static ValidationResultDto ToDto(ClassificationResultDocument doc, Guid engagementId) => new(
        doc.Id,
        engagementId,
        doc.IsBalanced,
        doc.TotalDebits,
        doc.TotalCredits,
        doc.Variance,
        doc.Classifications.Select(c => new ClassificationDto(
            c.AccountCode,
            c.AccountName,
            c.ClassifiedAs,
            c.Confidence,
            c.Flags.Cast<IReadOnlyDictionary<string, object>>().ToList(),
            c.Reasoning)).ToList(),
        doc.Summary,
        doc.FlaggedItems.Cast<IReadOnlyDictionary<string, object>>().ToList(),
        doc.ProcessedAt);
}
