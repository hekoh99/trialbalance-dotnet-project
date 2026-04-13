namespace TriBalance.Domain.Validation;

public interface IValidationJobRepository
{
    Task<ValidationJob?> GetByIdAsync(Guid id);
    Task<List<ValidationJob>> GetByEngagementIdAsync(Guid engagementId);
    Task AddAsync(ValidationJob job);
    Task UpdateAsync(ValidationJob job);
}
