namespace TriBalance.Application.Common.Messaging;

public interface ICommandDispatcher
{
    Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
}
