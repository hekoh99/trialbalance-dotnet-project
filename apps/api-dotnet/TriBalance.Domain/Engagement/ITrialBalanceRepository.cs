namespace TriBalance.Domain.Engagement;

public interface ITrialBalanceRepository
{
    Task<TrialBalance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(TrialBalance trialBalance, CancellationToken cancellationToken = default);
}
