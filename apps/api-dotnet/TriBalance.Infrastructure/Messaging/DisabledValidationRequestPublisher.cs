using Microsoft.Extensions.Logging;

namespace TriBalance.Infrastructure.Messaging;

/// <summary>
/// Fallback publisher used when Service Bus is not configured (e.g. running the
/// API locally without Azure). Throws a clear error so /validate returns a
/// useful 503 instead of crashing at DI resolution.
/// </summary>
public sealed class DisabledValidationRequestPublisher : IValidationRequestPublisher
{
    private readonly ILogger<DisabledValidationRequestPublisher> _logger;

    public DisabledValidationRequestPublisher(ILogger<DisabledValidationRequestPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(ValidationRequestMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogError("Service Bus not configured; cannot publish validation request for job {JobId}", message.ValidationJobId);
        throw new InvalidOperationException(
            "Service Bus connection string is not configured. Set Azure:ServiceBus:ConnectionString in appsettings.");
    }
}
