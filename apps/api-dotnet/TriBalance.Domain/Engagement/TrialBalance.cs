namespace TriBalance.Domain.Engagement;

using TriBalance.Domain.Common;

public class TrialBalance : Entity
{
    public Guid EngagementId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public DateTime SubmittedAt { get; private set; } = DateTime.UtcNow;
    public decimal TotalDebits { get; private set; }
    public decimal TotalCredits { get; private set; }
    public bool IsBalanced => Math.Abs(TotalDebits - TotalCredits) < BalanceTolerance.Epsilon;

    private readonly List<GLEntry> _glEntries = new();
    public IReadOnlyCollection<GLEntry> GlEntries => _glEntries.AsReadOnly();

    private TrialBalance() { }

    public TrialBalance(Guid engagementId, string fileName)
    {
        EngagementId = engagementId;
        FileName = fileName;
    }

    public void AddGlEntry(GLEntry entry)
    {
        _glEntries.Add(entry);
        RecalculateTotals();
    }

    public void AddGlEntries(IEnumerable<GLEntry> entries)
    {
        _glEntries.AddRange(entries);
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        TotalDebits = _glEntries.Sum(e => e.Debit);
        TotalCredits = _glEntries.Sum(e => e.Credit);
    }
}
