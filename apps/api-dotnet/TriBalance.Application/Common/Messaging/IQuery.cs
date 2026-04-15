namespace TriBalance.Application.Common.Messaging;

/// <summary>
/// Marker for a read-only use case returning <typeparamref name="TResult"/>.
/// Queries must never produce side effects; they are dispatched through
/// <see cref="IQueryDispatcher"/>. Kept separate from <see cref="ICommand{TResult}"/>
/// so pipeline behaviors (validation, transactions, retry) can differentiate.
/// </summary>
public interface IQuery<out TResult>
{
}
