namespace TriBalance.Infrastructure.Messaging;

/// <summary>
/// Payload received from tb-validation-result. Worker publishes
/// "processing" → "completed" | "failed" per job, which the result consumer
/// translates into an ApplyValidationResultCommand.
///
/// The outbound request payload is defined in Application
/// (ValidationRequestPayload) since handlers own the contract; this file only
/// carries the inbound message shape because it's an infrastructure-owned
/// deserialization detail that never leaks further up.
/// </summary>
public record ValidationResultMessage(
    Guid ValidationJobId,
    string Status,
    string? ErrorMessage);
