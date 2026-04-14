namespace TriBalance.Infrastructure.Messaging;

public class ServiceBusOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ValidationRequestQueue { get; set; } = "tb-validation-request";
    public string ValidationResultQueue { get; set; } = "tb-validation-result";
}
