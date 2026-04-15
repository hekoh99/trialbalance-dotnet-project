using Microsoft.EntityFrameworkCore;
using TriBalance.Domain.Engagement;

namespace TriBalance.Infrastructure.Persistence.PostgreSQL.Repositories;

public sealed class PostgresTrialBalanceRepository : ITrialBalanceRepository
{
    private readonly TriBalanceDbContext _context;

    public PostgresTrialBalanceRepository(TriBalanceDbContext context)
    {
        _context = context;
    }

    public Task<TrialBalance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.TrialBalances
            .Include(tb => tb.GlEntries)
            .FirstOrDefaultAsync(tb => tb.Id == id, cancellationToken);

    public async Task AddAsync(TrialBalance trialBalance, CancellationToken cancellationToken = default)
    {
        _context.TrialBalances.Add(trialBalance);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
