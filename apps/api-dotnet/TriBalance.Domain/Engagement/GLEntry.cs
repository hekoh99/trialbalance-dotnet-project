namespace TriBalance.Domain.Engagement;

using TriBalance.Domain.Common;

public class GLEntry : Entity
{
    public Guid TrialBalanceId { get; private set; }
    public Guid EngagementId { get; private set; }
    public string AccountCode { get; private set; } = string.Empty;
    public string AccountName { get; private set; } = string.Empty;
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }
    public decimal Balance { get; private set; }

    private GLEntry() { }

    public GLEntry(Guid trialBalanceId, Guid engagementId, string accountCode, string accountName, decimal debit, decimal credit)
    {
        TrialBalanceId = trialBalanceId;
        EngagementId = engagementId;
        AccountCode = accountCode;
        AccountName = accountName;
        Debit = debit;
        Credit = credit;
        Balance = debit - credit;
    }
}
