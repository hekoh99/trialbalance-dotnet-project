namespace TriBalance.Infrastructure.Messaging;

/// <summary>
/// Payload sent to tb-validation-request.
/// JSON wire format is camelCase (see ServiceBusJson). Worker's Pydantic model
/// is aliased to accept camelCase.
/// </summary>
public record ValidationRequestMessage(
    Guid EngagementId,
    Guid TrialBalanceId,
    Guid ValidationJobId,
    IReadOnlyList<GlEntryMessage> GlEntries);

public record GlEntryMessage(
    string AccountCode,
    string AccountName,
    decimal Debit,
    decimal Credit,
    decimal Balance);

/// <summary>
/// Payload received from tb-validation-result.
/// Worker publishes "processing" → "completed" | "failed" for each validation job,
/// driving the validation_jobs state machine and the SignalR dashboard updates.
/// </summary>
public record ValidationResultMessage(
    Guid ValidationJobId,
    string Status,
    string? ErrorMessage);
