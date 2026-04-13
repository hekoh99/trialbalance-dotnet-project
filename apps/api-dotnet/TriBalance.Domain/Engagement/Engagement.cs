namespace TriBalance.Domain.Engagement;

using TriBalance.Domain.Common;

public class Engagement : AggregateRoot
{
    public string ClientName { get; private set; } = string.Empty;
    public DateTime FiscalYearEnd { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private readonly List<TrialBalance> _trialBalances = new();
    public IReadOnlyCollection<TrialBalance> TrialBalances => _trialBalances.AsReadOnly();

    private Engagement() { }

    public Engagement(string clientName, DateTime fiscalYearEnd)
    {
        ClientName = clientName;
        FiscalYearEnd = fiscalYearEnd;
    }

    public TrialBalance AddTrialBalance(string fileName)
    {
        var tb = new TrialBalance(Id, fileName);
        _trialBalances.Add(tb);
        UpdatedAt = DateTime.UtcNow;
        return tb;
    }
}
