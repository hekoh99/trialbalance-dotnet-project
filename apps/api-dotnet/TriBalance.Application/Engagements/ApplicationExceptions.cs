namespace TriBalance.Application.Engagements;

public class EngagementNotFoundException : Exception
{
    public Guid EngagementId { get; }
    public EngagementNotFoundException(Guid engagementId)
        : base($"Engagement {engagementId} not found")
    {
        EngagementId = engagementId;
    }
}

public class TrialBalanceNotFoundException : Exception
{
    public Guid TrialBalanceId { get; }
    public TrialBalanceNotFoundException(Guid trialBalanceId)
        : base($"Trial balance {trialBalanceId} not found")
    {
        TrialBalanceId = trialBalanceId;
    }
}

public class InvalidCsvException : Exception
{
    public InvalidCsvException(string message) : base(message) { }
}
