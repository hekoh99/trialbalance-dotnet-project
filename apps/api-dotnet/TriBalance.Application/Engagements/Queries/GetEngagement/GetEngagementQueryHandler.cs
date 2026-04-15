using TriBalance.Application.Common.Messaging;
using TriBalance.Domain.Engagement;

namespace TriBalance.Application.Engagements.Queries.GetEngagement;

internal sealed class GetEngagementQueryHandler
    : IQueryHandler<GetEngagementQuery, EngagementDto?>
{
    private readonly IEngagementRepository _repository;

    public GetEngagementQueryHandler(IEngagementRepository repository)
    {
        _repository = repository;
    }

    public async Task<EngagementDto?> Handle(GetEngagementQuery query, CancellationToken cancellationToken)
    {
        var engagement = await _repository.GetByIdAsync(query.EngagementId);
        return engagement is null ? null : EngagementMapper.ToDto(engagement);
    }
}
