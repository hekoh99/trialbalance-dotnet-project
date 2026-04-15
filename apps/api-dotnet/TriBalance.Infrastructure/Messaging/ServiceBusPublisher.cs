using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using TriBalance.Application.Validation;

namespace TriBalance.Infrastructure.Messaging;

/// <summary>
/// Implements Application.Validation.IValidationRequestPublisher on Service Bus.
/// camelCase on the wire so the Python Worker's Pydantic models deserialize
/// directly. Sender is created per call — SDK pools under the hood.
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

    public async Task PublishAsync(ValidationRequestPayload payload, CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.Serialize(payload, ServiceBusJson.Options);
        var sbMessage = new ServiceBusMessage(body) { ContentType = "application/json" };

        await using var sender = _client.CreateSender(_options.ValidationRequestQueue);
        await sender.SendMessageAsync(sbMessage, cancellationToken);

        _logger.LogInformation(
            "Published ValidationRequest engagement={EngagementId} job={JobId}",
            payload.EngagementId, payload.ValidationJobId);
    }
}
