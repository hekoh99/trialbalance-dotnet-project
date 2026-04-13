namespace TriBalance.Domain.Validation;

using TriBalance.Domain.Common;

public class ValidationJob : Entity
{
    public Guid EngagementId { get; private set; }
    public Guid TrialBalanceId { get; private set; }
    public ValidationJobStatus Status { get; private set; } = ValidationJobStatus.Queued;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private ValidationJob() { }

    public ValidationJob(Guid engagementId, Guid trialBalanceId)
    {
        EngagementId = engagementId;
        TrialBalanceId = trialBalanceId;
    }

    public void MarkProcessing()
    {
        Status = ValidationJobStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        Status = ValidationJobStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = ValidationJobStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
