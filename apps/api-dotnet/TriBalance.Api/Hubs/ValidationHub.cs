using Microsoft.AspNetCore.SignalR;

namespace TriBalance.Api.Hubs;

/// <summary>
/// SignalR hub used by the Angular dashboard to follow a single engagement's
/// validation lifecycle in real time. Clients call JoinEngagement(engagementId)
/// after connecting; the server pushes ValidationStatusUpdated events to that group.
/// </summary>
public class ValidationHub : Hub
{
    public Task JoinEngagement(string engagementId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, engagementId);

    public Task LeaveEngagement(string engagementId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, engagementId);
}
