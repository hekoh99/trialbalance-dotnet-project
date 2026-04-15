using Microsoft.EntityFrameworkCore;
using TriBalance.Domain.Engagement;

namespace TriBalance.Infrastructure.Persistence.PostgreSQL.Repositories;

public class PostgresEngagementRepository : IEngagementRepository
{
    private readonly TriBalanceDbContext _context;

    public PostgresEngagementRepository(TriBalanceDbContext context)
    {
        _context = context;
    }

    public async Task<Engagement?> GetByIdAsync(Guid id)
    {
        return await _context.Engagements
            .Include(e => e.TrialBalances)
                .ThenInclude(tb => tb.GlEntries)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Engagement>> GetAllAsync()
    {
        return await _context.Engagements
            .Include(e => e.TrialBalances)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Engagement engagement)
    {
        await _context.Engagements.AddAsync(engagement);
        await _context.SaveChangesAsync();
    }
}
