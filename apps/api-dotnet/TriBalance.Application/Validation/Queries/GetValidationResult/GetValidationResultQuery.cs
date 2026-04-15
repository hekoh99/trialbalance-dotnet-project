using TriBalance.Application.Common.Messaging;

namespace TriBalance.Application.Validation.Queries.GetValidationResult;

public record GetValidationResultQuery(Guid EngagementId) : IQuery<ValidationResultDto?>;
