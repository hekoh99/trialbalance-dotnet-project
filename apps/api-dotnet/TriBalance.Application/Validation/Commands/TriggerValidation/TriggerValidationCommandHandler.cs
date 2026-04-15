using TriBalance.Application.Common.Messaging;
using TriBalance.Application.Engagements;
using TriBalance.Domain.Engagement;
using TriBalance.Domain.Validation;

namespace TriBalance.Application.Validation.Commands.TriggerValidation;

/// <summary>
/// Creates a Queued validation_job, publishes the request to the worker queue,
/// and pushes the initial status over SignalR. Each call produces a fresh job
/// so retries (Scenario 3) just re-invoke this handler.
/// </summary>
internal sealed class TriggerValidationCommandHandler
    : ICommandHandler<TriggerValidationCommand, ValidationJobDto>
{
    private readonly ITrialBalanceRepository _trialBalances;
    private readonly IValidationJobRepository _jobs;
    private readonly IValidationRequestPublisher _publisher;
    private readonly IValidationStatusNotifier _notifier;

    public TriggerValidationCommandHandler(
        ITrialBalanceRepository trialBalances,
        IValidationJobRepository jobs,
        IValidationRequestPublisher publisher,
        IValidationStatusNotifier notifier)
    {
        _trialBalances = trialBalances;
        _jobs = jobs;
        _publisher = publisher;
        _notifier = notifier;
    }

    public async Task<ValidationJobDto> Handle(TriggerValidationCommand command, CancellationToken cancellationToken)
    {
        var tb = await _trialBalances.GetByIdAsync(command.TrialBalanceId, cancellationToken)
            ?? throw new TrialBalanceNotFoundException(command.TrialBalanceId);

        if (tb.EngagementId != command.EngagementId)
            throw new TrialBalanceNotFoundException(command.TrialBalanceId);
        if (tb.GlEntries.Count == 0)
            throw new InvalidOperationException("Trial balance has no GL entries");

        var job = new ValidationJob(command.EngagementId, tb.Id);
        await _jobs.AddAsync(job);

        var payload = new ValidationRequestPayload(
            command.EngagementId,
            tb.Id,
            job.Id,
            tb.GlEntries.Select(g => new ValidationRequestGlEntry(
                g.AccountCode, g.AccountName, g.Debit, g.Credit, g.Balance)).ToList());

        await _publisher.PublishAsync(payload, cancellationToken);
        await _notifier.PushAsync(command.EngagementId, job.Id, nameof(ValidationJobStatus.Queued), cancellationToken: cancellationToken);

        return ValidationMapper.ToDto(job);
    }
}
