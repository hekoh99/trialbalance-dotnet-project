using TriBalance.Application.Common.Messaging;

namespace TriBalance.Application.Engagements.Queries.GetEngagement;

public record GetEngagementQuery(Guid EngagementId) : IQuery<EngagementDto?>;
