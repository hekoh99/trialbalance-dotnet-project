using Microsoft.Extensions.DependencyInjection;

namespace TriBalance.Application.Common.Messaging;

public sealed class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public QueryDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResult> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        var handler = _serviceProvider.GetRequiredService(handlerType);

        RequestHandlerDelegate<TResult> terminal = () =>
            (Task<TResult>)handlerType
                .GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.Handle))!
                .Invoke(handler, new object[] { query, cancellationToken })!;

        return PipelineRunner.Run(_serviceProvider, query, terminal, cancellationToken);
    }
}
