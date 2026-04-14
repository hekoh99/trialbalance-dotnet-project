using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TriBalance.Domain.Validation;

namespace TriBalance.Infrastructure.Messaging;

/// <summary>
/// Long-running background service that consumes worker results from
/// tb-validation-result, transitions validation_jobs state, and notifies the
/// dashboard through the injected status notifier.
///
/// Runs inside the API process so a single hub instance is shared with the
/// HTTP pipeline — this is what allows the notifier to push to SignalR
/// groups that were joined via the /hubs/validation endpoint.
/// </summary>
public sealed class ValidationResultConsumer : BackgroundService
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IValidationStatusNotifierAdapter _notifier;
    private readonly ILogger<ValidationResultConsumer> _logger;

    public ValidationResultConsumer(
        ServiceBusClient client,
        ServiceBusOptions options,
        IServiceScopeFactory scopeFactory,
        IValidationStatusNotifierAdapter notifier,
        ILogger<ValidationResultConsumer> logger)
    {
        _client = client;
        _options = options;
        _scopeFactory = scopeFactory;
        _notifier = notifier;
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
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
        finally
        {
            await processor.StopProcessingAsync(CancellationToken.None);
            await processor.DisposeAsync();
        }
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();
        ValidationResultMessage? result;
        try
        {
            result = JsonSerializer.Deserialize<ValidationResultMessage>(body, ServiceBusJson.Options);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Malformed result payload, dead-lettering. Body: {Body}", body);
            await args.DeadLetterMessageAsync(args.Message, "MalformedPayload", ex.Message);
            return;
        }

        if (result is null)
        {
            await args.DeadLetterMessageAsync(args.Message, "NullPayload");
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IValidationJobRepository>();

        var job = await jobRepo.GetByIdAsync(result.ValidationJobId);
        if (job is null)
        {
            _logger.LogWarning("Received result for unknown job {JobId}", result.ValidationJobId);
            await args.CompleteMessageAsync(args.Message);
            return;
        }

        ApplyStatus(job, result);
        await jobRepo.UpdateAsync(job);

        await _notifier.PushAsync(
            job.EngagementId,
            job.Id,
            job.Status.ToString(),
            job.ErrorMessage);

        await args.CompleteMessageAsync(args.Message);
    }

    private static void ApplyStatus(ValidationJob job, ValidationResultMessage msg)
    {
        switch (msg.Status.ToLowerInvariant())
        {
            case "processing":
                job.MarkProcessing();
                break;
            case "completed":
                job.MarkCompleted();
                break;
            case "failed":
                job.MarkFailed(msg.ErrorMessage ?? "Worker reported failure without message");
                break;
            default:
                job.MarkFailed($"Unknown status: {msg.Status}");
                break;
        }
    }
}

/// <summary>
/// Tiny seam so Infrastructure can push status updates without referencing
/// the Api project (which owns the SignalR hub).
/// </summary>
public interface IValidationStatusNotifierAdapter
{
    Task PushAsync(
        Guid engagementId,
        Guid validationJobId,
        string status,
        string? errorMessage,
        CancellationToken cancellationToken = default);
}
