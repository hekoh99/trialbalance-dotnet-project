using Microsoft.Extensions.Logging;
using TriBalance.Application.Common.Messaging;
using TriBalance.Domain.Validation;

namespace TriBalance.Application.Validation.Commands.ApplyValidationResult;

internal sealed class ApplyValidationResultCommandHandler
    : ICommandHandler<ApplyValidationResultCommand, Unit>
{
    private readonly IValidationJobRepository _jobs;
    private readonly IValidationStatusNotifier _notifier;
    private readonly ILogger<ApplyValidationResultCommandHandler> _logger;

    public ApplyValidationResultCommandHandler(
        IValidationJobRepository jobs,
        IValidationStatusNotifier notifier,
        ILogger<ApplyValidationResultCommandHandler> logger)
    {
        _jobs = jobs;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<Unit> Handle(ApplyValidationResultCommand command, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(command.ValidationJobId);
        if (job is null)
        {
            _logger.LogWarning("Result received for unknown validation job {JobId}", command.ValidationJobId);
            return Unit.Value;
        }

        switch (command.Status.ToLowerInvariant())
        {
            case "processing":
                job.MarkProcessing();
                break;
            case "completed":
                job.MarkCompleted();
                break;
            case "failed":
                job.MarkFailed(command.ErrorMessage ?? "Worker reported failure without message");
                break;
            default:
                job.MarkFailed($"Unknown status: {command.Status}");
                break;
        }

        await _jobs.UpdateAsync(job);
        await _notifier.PushAsync(job.EngagementId, job.Id, job.Status.ToString(), job.ErrorMessage, cancellationToken);

        return Unit.Value;
    }
}
