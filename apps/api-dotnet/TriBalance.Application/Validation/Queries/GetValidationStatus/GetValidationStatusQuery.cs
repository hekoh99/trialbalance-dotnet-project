using TriBalance.Application.Common.Messaging;

namespace TriBalance.Application.Validation.Queries.GetValidationStatus;

public record GetValidationStatusQuery(Guid EngagementId) : IQuery<ValidationJobDto?>;
