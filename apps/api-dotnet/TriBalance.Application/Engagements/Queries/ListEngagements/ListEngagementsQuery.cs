using TriBalance.Application.Common.Messaging;

namespace TriBalance.Application.Engagements.Queries.ListEngagements;

public record ListEngagementsQuery : IQuery<IReadOnlyList<EngagementDto>>;
