using Microsoft.EntityFrameworkCore;
using TriBalance.Domain.Validation;

namespace TriBalance.Infrastructure.Persistence.PostgreSQL.Repositories;

public class PostgresValidationJobRepository : IValidationJobRepository
{
    private readonly TriBalanceDbContext _context;

    public PostgresValidationJobRepository(TriBalanceDbContext context)
    {
        _context = context;
    }

    public async Task<ValidationJob?> GetByIdAsync(Guid id)
    {
        return await _context.ValidationJobs.FindAsync(id);
    }

    public async Task<List<ValidationJob>> GetByEngagementIdAsync(Guid engagementId)
    {
        return await _context.ValidationJobs
            .Where(j => j.EngagementId == engagementId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(ValidationJob job)
    {
        await _context.ValidationJobs.AddAsync(job);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ValidationJob job)
    {
        _context.ValidationJobs.Update(job);
        await _context.SaveChangesAsync();
    }
}
