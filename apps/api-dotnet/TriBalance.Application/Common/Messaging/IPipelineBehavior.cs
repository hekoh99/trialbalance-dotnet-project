namespace TriBalance.Application.Common.Messaging;

public delegate Task<TResult> RequestHandlerDelegate<TResult>();

/// <summary>
/// Cross-cutting concern that wraps handler execution — logging, validation,
/// transactions, retry. Multiple behaviors compose into a pipeline in the
/// order they're registered in DI (first-registered = outermost).
///
/// Used by both command and query dispatchers; a behavior can opt-out by
/// checking the generic arg or marker interface of <typeparamref name="TRequest"/>.
/// </summary>
public interface IPipelineBehavior<in TRequest, TResult>
    where TRequest : notnull
{
    Task<TResult> Handle(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken);
}
