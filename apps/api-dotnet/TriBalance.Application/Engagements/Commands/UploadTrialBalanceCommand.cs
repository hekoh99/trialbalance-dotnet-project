namespace TriBalance.Application.Engagements.Commands;

public record UploadTrialBalanceCommand(Guid EngagementId, string FileName, Stream FileStream);
