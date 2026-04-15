using Microsoft.Extensions.DependencyInjection;

namespace TriBalance.Application.Common.Messaging;

/// <summary>
/// Wraps a terminal handler invocation with all IPipelineBehavior&lt;TRequest,TResult&gt;
/// registered in DI. Behaviors compose outermost-first so the first-registered
/// logs/authorizes before validation/transaction behaviors run.
/// </summary>
internal static class PipelineRunner
{
    public static Task<TResult> Run<TResult>(
        IServiceProvider serviceProvider,
        object request,
        RequestHandlerDelegate<TResult> terminal,
        CancellationToken cancellationToken)
    {
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(request.GetType(), typeof(TResult));
        var behaviors = ((IEnumerable<object>)serviceProvider.GetServices(behaviorType)).Reverse().ToList();

        if (behaviors.Count == 0)
            return terminal();

        RequestHandlerDelegate<TResult> pipeline = terminal;
        foreach (var behavior in behaviors)
        {
            var current = pipeline; // closure capture
            pipeline = () => (Task<TResult>)behaviorType
                .GetMethod(nameof(IPipelineBehavior<object, TResult>.Handle))!
                .Invoke(behavior, new object[] { request, current, cancellationToken })!;
        }

        return pipeline();
    }
}
