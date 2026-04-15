using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TriBalance.Application.Common.Messaging;
using TriBalance.Application.Validation.Commands.ApplyValidationResult;

namespace TriBalance.Infrastructure.Messaging;

/// <summary>
/// Background consumer for tb-validation-result. Doesn't know anything about
/// job state transitions or SignalR — it just turns each message into an
/// ApplyValidationResultCommand and dispatches. All transition logic + dashboard
/// push lives in the command handler so HTTP-triggered and Service-Bus-triggered
/// paths share exactly one implementation (and the same logging pipeline).
/// </summary>
public sealed class ValidationResultConsumer : BackgroundService
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ValidationResultConsumer> _logger;

    public ValidationResultConsumer(
        ServiceBusClient client,
        ServiceBusOptions options,
        IServiceScopeFactory scopeFactory,
        ILogger<ValidationResultConsumer> logger)
    {
        _client = client;
        _options = options;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processor = _client.CreateProcessor(_options.ValidationResultQueue, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 4,
            AutoCompleteMessages = false,
        });

        processor.ProcessMessageAsync += HandleMessageAsync;
        processor.ProcessErrorAsync += args =>
        {
            _logger.LogError(args.Exception, "Service Bus processor error");
            return Task.CompletedTask;
        };

        await processor.StartProcessingAsync(stoppingToken);
        _logger.LogInformation("ValidationResultConsumer listening on {Queue}", _options.ValidationResultQueue);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { /* graceful shutdown */ }
        finally
        {
            await processor.StopProcessingAsync(CancellationToken.None);
            await processor.DisposeAsync();
        }
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();
        ValidationResultMessage? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<ValidationResultMessage>(body, ServiceBusJson.Options);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Malformed result payload; dead-lettering. Body: {Body}", body);
            await args.DeadLetterMessageAsync(args.Message, "MalformedPayload", ex.Message);
            return;
        }

        if (parsed is null)
        {
            await args.DeadLetterMessageAsync(args.Message, "NullPayload");
            return;
        }

        // Scope-per-message so scoped services (DbContext, repositories) have
        // their own unit of work. Dispatcher + handler do the rest.
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        await dispatcher.Send(new ApplyValidationResultCommand(
            parsed.ValidationJobId,
            parsed.Status,
            parsed.ErrorMessage));

        await args.CompleteMessageAsync(args.Message);
    }
}
