namespace TriBalance.Infrastructure.Persistence.CosmosDB;

public class CosmosOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "tribalance";
    public string ClassificationContainer { get; set; } = "classification-results";
    public string ValidationReportContainer { get; set; } = "validation-reports";
}
