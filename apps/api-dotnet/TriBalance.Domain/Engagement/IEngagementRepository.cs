namespace TriBalance.Domain.Engagement;

public interface IEngagementRepository
{
    Task<Engagement?> GetByIdAsync(Guid id);
    Task<List<Engagement>> GetAllAsync();
    Task AddAsync(Engagement engagement);
    Task UpdateAsync(Engagement engagement);
}
