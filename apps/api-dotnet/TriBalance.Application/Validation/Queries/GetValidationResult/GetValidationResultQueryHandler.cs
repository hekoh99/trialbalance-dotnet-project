using TriBalance.Application.Common.Messaging;

namespace TriBalance.Application.Validation.Queries.GetValidationResult;

internal sealed class GetValidationResultQueryHandler
    : IQueryHandler<GetValidationResultQuery, ValidationResultDto?>
{
    private readonly IValidationResultReader _reader;

    public GetValidationResultQueryHandler(IValidationResultReader reader)
    {
        _reader = reader;
    }

    public Task<ValidationResultDto?> Handle(GetValidationResultQuery query, CancellationToken cancellationToken) =>
        _reader.GetLatestByEngagementAsync(query.EngagementId, cancellationToken);
}
