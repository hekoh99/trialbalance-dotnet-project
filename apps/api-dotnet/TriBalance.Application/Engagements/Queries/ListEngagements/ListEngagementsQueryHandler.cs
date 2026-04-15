using TriBalance.Application.Common.Messaging;
using TriBalance.Domain.Engagement;

namespace TriBalance.Application.Engagements.Queries.ListEngagements;

internal sealed class ListEngagementsQueryHandler
    : IQueryHandler<ListEngagementsQuery, IReadOnlyList<EngagementDto>>
{
    private readonly IEngagementRepository _repository;

    public ListEngagementsQueryHandler(IEngagementRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<EngagementDto>> Handle(ListEngagementsQuery query, CancellationToken cancellationToken)
    {
        var engagements = await _repository.GetAllAsync();
        return engagements.Select(EngagementMapper.ToDto).ToList();
    }
}
