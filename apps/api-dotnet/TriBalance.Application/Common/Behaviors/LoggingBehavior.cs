using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TriBalance.Application.Common.Messaging;

namespace TriBalance.Application.Common.Behaviors;

/// <summary>
/// Wraps every command and query dispatch with structured logging so the
/// observability story is uniform regardless of entry point (HTTP endpoint,
/// Service Bus consumer, CLI, etc.). Registered as an open generic IPipelineBehavior.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResult>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResult>> logger)
    {
        _logger = logger;
    }

    public async Task<TResult> Handle(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await next();
            _logger.LogInformation("{Request} handled in {Elapsed}ms", name, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Request} failed after {Elapsed}ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
