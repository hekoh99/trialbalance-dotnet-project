using TriBalance.Infrastructure.Messaging;

namespace TriBalance.Api.Hubs;

/// <summary>
/// Bridges the Infrastructure-side adapter interface to the Api's SignalR notifier.
/// Lets the BackgroundService in Infrastructure push updates without taking a
/// reference on SignalR types directly.
/// </summary>
public sealed class SignalRValidationStatusNotifierAdapter : IValidationStatusNotifierAdapter
{
    private readonly IValidationStatusNotifier _inner;

    public SignalRValidationStatusNotifierAdapter(IValidationStatusNotifier inner)
    {
        _inner = inner;
    }

    public Task PushAsync(
        Guid engagementId,
        Guid validationJobId,
        string status,
        string? errorMessage,
        CancellationToken cancellationToken = default) =>
        _inner.PushAsync(engagementId, validationJobId, status, errorMessage, cancellationToken);
}
