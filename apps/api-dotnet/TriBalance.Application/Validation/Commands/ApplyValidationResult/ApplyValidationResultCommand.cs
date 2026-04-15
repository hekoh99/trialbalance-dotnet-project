using TriBalance.Application.Common.Messaging;

namespace TriBalance.Application.Validation.Commands.ApplyValidationResult;

/// <summary>
/// Dispatched by the Service Bus result consumer for every worker status
/// message. Kept as a single command so the HTTP pipeline and the background
/// consumer share logging/validation/transaction concerns.
/// </summary>
public record ApplyValidationResultCommand(
    Guid ValidationJobId,
    string Status,
    string? ErrorMessage) : ICommand;
