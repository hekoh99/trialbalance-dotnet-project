using Microsoft.AspNetCore.SignalR;

namespace TriBalance.Api.Hubs;

public interface IValidationStatusNotifier
{
    Task PushAsync(
        Guid engagementId,
        Guid validationJobId,
        string status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);
}

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
        CancellationToken cancellationToken = default)
    {
        return _hub.Clients
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
}
