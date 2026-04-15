using TriBalance.Application.Common.Messaging;
using TriBalance.Domain.Validation;

namespace TriBalance.Application.Validation.Queries.GetValidationStatus;

internal sealed class GetValidationStatusQueryHandler
    : IQueryHandler<GetValidationStatusQuery, ValidationJobDto?>
{
    private readonly IValidationJobRepository _jobs;

    public GetValidationStatusQueryHandler(IValidationJobRepository jobs)
    {
        _jobs = jobs;
    }

    public async Task<ValidationJobDto?> Handle(GetValidationStatusQuery query, CancellationToken cancellationToken)
    {
        var jobs = await _jobs.GetByEngagementIdAsync(query.EngagementId);
        var latest = jobs.FirstOrDefault();
        return latest is null ? null : ValidationMapper.ToDto(latest);
    }
}
