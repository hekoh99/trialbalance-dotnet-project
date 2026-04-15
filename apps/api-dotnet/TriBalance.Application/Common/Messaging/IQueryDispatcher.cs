namespace TriBalance.Application.Common.Messaging;

public interface IQueryDispatcher
{
    Task<TResult> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}
