using Microsoft.AspNetCore.SignalR;
using TriBalance.Application.Validation;

namespace TriBalance.Api.Hubs;

/// <summary>
/// Implements Application's IValidationStatusNotifier on top of SignalR. Pushes
/// ValidationStatusUpdated events to the engagement-scoped group so only
/// clients watching that engagement receive the update.
/// </summary>
public sealed class SignalRValidationStatusNotifier : IValidationStatusNotifier
{
    private readonly IHubContext<ValidationHub> _hub;

    public SignalRValidationStatusNotifier(IHubContext<ValidationHub> hub)
    {
        _hub = hub;
    }

    public Task PushAsync(
        Guid engagementId,
        Guid validationJobId,
        string status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default) =>
        _hub.Clients
            .Group(engagementId.ToString())
            .SendAsync("ValidationStatusUpdated", new
            {
                engagementId,
                validationJobId,
                status,
                errorMessage,
                timestamp = DateTime.UtcNow,
            }, cancellationToken);
}
