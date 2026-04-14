using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace TriBalance.Infrastructure.Messaging;

public interface IValidationRequestPublisher
{
    Task PublishAsync(ValidationRequestMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Publishes validation requests to the Service Bus queue that the Python Worker consumes.
/// Uses a single ServiceBusClient (registered as singleton) and creates a sender per call
/// (lightweight — SB SDK pools them internally).
/// </summary>
public sealed class ServiceBusValidationRequestPublisher : IValidationRequestPublisher
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusOptions _options;
    private readonly ILogger<ServiceBusValidationRequestPublisher> _logger;

    public ServiceBusValidationRequestPublisher(
        ServiceBusClient client,
        ServiceBusOptions options,
        ILogger<ServiceBusValidationRequestPublisher> logger)
    {
        _client = client;
        _options = options;
        _logger = logger;
    }

    public async Task PublishAsync(ValidationRequestMessage message, CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.Serialize(message, ServiceBusJson.Options);
        var sbMessage = new ServiceBusMessage(body) { ContentType = "application/json" };

        await using var sender = _client.CreateSender(_options.ValidationRequestQueue);
        await sender.SendMessageAsync(sbMessage, cancellationToken);

        _logger.LogInformation(
            "Published ValidationRequest engagement={EngagementId} job={JobId}",
            message.EngagementId, message.ValidationJobId);
    }
}
