using Microsoft.Extensions.DependencyInjection;

namespace TriBalance.Application.Common.Messaging;

/// <summary>
/// Resolves the correct ICommandHandler&lt;TCommand, TResult&gt; for a given command
/// at runtime and executes it through any registered IPipelineBehavior&lt;,&gt; layers.
///
/// Using IServiceProvider rather than constructor-injecting every handler keeps
/// the dispatcher open to new commands without churn.
/// </summary>
public sealed class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Build the concrete handler type: ICommandHandler<TCommand, TResult>.
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        var handler = _serviceProvider.GetRequiredService(handlerType);

        // Terminal step — invoke the concrete handler via its Handle method.
        // dynamic dispatch so we don't need to hardcode command type in generics.
        RequestHandlerDelegate<TResult> terminal = () =>
            (Task<TResult>)handlerType
                .GetMethod(nameof(ICommandHandler<ICommand<TResult>, TResult>.Handle))!
                .Invoke(handler, new object[] { command, cancellationToken })!;

        return PipelineRunner.Run(_serviceProvider, command, terminal, cancellationToken);
    }
}
