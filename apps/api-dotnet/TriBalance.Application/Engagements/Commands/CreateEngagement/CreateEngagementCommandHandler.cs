using TriBalance.Application.Common.Messaging;
using TriBalance.Domain.Engagement;

namespace TriBalance.Application.Engagements.Commands.CreateEngagement;

internal sealed class CreateEngagementCommandHandler
    : ICommandHandler<CreateEngagementCommand, EngagementDto>
{
    private readonly IEngagementRepository _repository;

    public CreateEngagementCommandHandler(IEngagementRepository repository)
    {
        _repository = repository;
    }

    public async Task<EngagementDto> Handle(CreateEngagementCommand command, CancellationToken cancellationToken)
    {
        var engagement = new Engagement(command.ClientName, command.FiscalYearEnd);
        await _repository.AddAsync(engagement);
        return EngagementMapper.ToDto(engagement);
    }
}
