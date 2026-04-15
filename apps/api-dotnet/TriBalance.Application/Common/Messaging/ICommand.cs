namespace TriBalance.Application.Common.Messaging;

/// <summary>
/// Marker for an imperative use case that returns <typeparamref name="TResult"/>.
/// Commands carry the data needed to perform a side-effectful operation
/// (create, update, delete, trigger, …) and are dispatched through
/// <see cref="ICommandDispatcher"/>.
/// </summary>
public interface ICommand<out TResult>
{
}

/// <summary>Command that doesn't need to return anything meaningful.</summary>
public interface ICommand : ICommand<Unit>
{
}
