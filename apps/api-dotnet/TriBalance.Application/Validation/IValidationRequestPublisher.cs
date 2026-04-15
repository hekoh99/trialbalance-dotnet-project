namespace TriBalance.Application.Validation;

/// <summary>
/// Outbound port for publishing validation requests to whatever transport the
/// Worker consumes (Service Bus in production; Infrastructure owns the impl).
/// Living in Application lets handlers stay transport-agnostic.
/// </summary>
public interface IValidationRequestPublisher
{
    Task PublishAsync(ValidationRequestPayload payload, CancellationToken cancellationToken = default);
}

public record ValidationRequestPayload(
    Guid EngagementId,
    Guid TrialBalanceId,
    Guid ValidationJobId,
    IReadOnlyList<ValidationRequestGlEntry> GlEntries);

public record ValidationRequestGlEntry(
    string AccountCode,
    string AccountName,
    decimal Debit,
    decimal Credit,
    decimal Balance);
