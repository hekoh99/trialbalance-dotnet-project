namespace TriBalance.Application.Common.Messaging;

/// <summary>
/// Non-value return type for commands that don't yield a result.
/// Mirrors MediatR's Unit so handlers can uniformly return Task&lt;TResult&gt;
/// without a separate no-result interface. Prefer returning a meaningful DTO
/// where possible; reach for Unit only when truly nothing needs to flow back.
/// </summary>
public readonly record struct Unit
{
    public static readonly Unit Value = default;
}
