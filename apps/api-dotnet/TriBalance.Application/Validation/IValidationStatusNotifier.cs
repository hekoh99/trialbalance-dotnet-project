namespace TriBalance.Application.Validation;

/// <summary>
/// Outbound port for pushing validation status transitions to connected
/// clients. Infrastructure/Api provides the SignalR-backed implementation.
/// </summary>
public interface IValidationStatusNotifier
{
    Task PushAsync(
        Guid engagementId,
        Guid validationJobId,
        string status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);
}
