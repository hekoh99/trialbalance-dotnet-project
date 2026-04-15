using TriBalance.Application.Common.Messaging;
using TriBalance.Domain.Engagement;

namespace TriBalance.Application.Engagements.Commands.UploadTrialBalance;

/// <summary>
/// Parses the uploaded CSV, builds a TrialBalance aggregate with its GL entries,
/// and persists via ITrialBalanceRepository. Returns a summary DTO so the API
/// response — and the caller kicking off /validate next — has everything it
/// needs without a second round trip.
/// </summary>
internal sealed class UploadTrialBalanceCommandHandler
    : ICommandHandler<UploadTrialBalanceCommand, TrialBalanceSummaryDto>
{
    private readonly IGlEntryCsvParser _parser;
    private readonly ITrialBalanceRepository _trialBalanceRepository;
    private readonly IEngagementRepository _engagementRepository;

    public UploadTrialBalanceCommandHandler(
        IGlEntryCsvParser parser,
        ITrialBalanceRepository trialBalanceRepository,
        IEngagementRepository engagementRepository)
    {
        _parser = parser;
        _trialBalanceRepository = trialBalanceRepository;
        _engagementRepository = engagementRepository;
    }

    public async Task<TrialBalanceSummaryDto> Handle(UploadTrialBalanceCommand command, CancellationToken cancellationToken)
    {
        var engagement = await _engagementRepository.GetByIdAsync(command.EngagementId)
            ?? throw new EngagementNotFoundException(command.EngagementId);

        var parsed = _parser.Parse(command.FileStream);
        if (parsed.Count == 0)
            throw new InvalidCsvException("CSV contains no data rows");

        var trialBalance = new TrialBalance(command.EngagementId, command.FileName);
        var entries = parsed.Select(p => new GLEntry(
            trialBalance.Id,
            command.EngagementId,
            p.AccountCode,
            p.AccountName,
            p.Debit,
            p.Credit)).ToList();
        trialBalance.AddGlEntries(entries);

        await _trialBalanceRepository.AddAsync(trialBalance);

        return EngagementMapper.ToSummary(trialBalance);
    }
}
