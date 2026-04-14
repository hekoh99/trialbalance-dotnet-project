using System.Text.Json.Serialization;

namespace TriBalance.Infrastructure.Persistence.CosmosDB;

/// <summary>
/// Shape of a document in classification-results, written by the Python Worker.
/// Partition key is engagementId.
/// </summary>
public class ClassificationResultDocument
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("engagementId")] public string EngagementId { get; set; } = string.Empty;
    [JsonPropertyName("isBalanced")] public bool IsBalanced { get; set; }
    [JsonPropertyName("totalDebits")] public decimal TotalDebits { get; set; }
    [JsonPropertyName("totalCredits")] public decimal TotalCredits { get; set; }
    [JsonPropertyName("variance")] public decimal Variance { get; set; }
    [JsonPropertyName("classifications")] public List<ClassificationDocument> Classifications { get; set; } = new();
    [JsonPropertyName("summary")] public Dictionary<string, int> Summary { get; set; } = new();
    [JsonPropertyName("flaggedItems")] public List<Dictionary<string, object>> FlaggedItems { get; set; } = new();
    [JsonPropertyName("processedAt")] public DateTime ProcessedAt { get; set; }
}

public class ClassificationDocument
{
    [JsonPropertyName("accountCode")] public string AccountCode { get; set; } = string.Empty;
    [JsonPropertyName("accountName")] public string AccountName { get; set; } = string.Empty;
    [JsonPropertyName("classifiedAs")] public string ClassifiedAs { get; set; } = string.Empty;
    [JsonPropertyName("confidence")] public double Confidence { get; set; }
    [JsonPropertyName("flags")] public List<Dictionary<string, object>> Flags { get; set; } = new();
    [JsonPropertyName("reasoning")] public string? Reasoning { get; set; }
}
