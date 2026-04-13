using System.Globalization;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using TriBalance.Domain.Engagement;
using TriBalance.Infrastructure.Persistence.PostgreSQL;
using TriBalance.Infrastructure.Persistence.PostgreSQL.CsvParsing;

namespace TriBalance.Api.Endpoints;

public static class TrialBalanceEndpoints
{
    public record TrialBalanceResponse(
        Guid Id,
        Guid EngagementId,
        string FileName,
        DateTime SubmittedAt,
        decimal TotalDebits,
        decimal TotalCredits,
        bool IsBalanced,
        int GlEntryCount);

    public static void MapTrialBalanceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/engagements/{engagementId:guid}").WithTags("Trial Balance");

        group.MapPost("/trial-balance", async (Guid engagementId, IFormFile file, TriBalanceDbContext db) =>
        {
            var engagementExists = await db.Engagements.AnyAsync(e => e.Id == engagementId);
            if (!engagementExists)
                return Results.NotFound(new { message = "Engagement not found" });

            if (file.Length == 0)
                return Results.BadRequest(new { message = "File is empty" });

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<GlEntryCsvMap>();

            var records = csv.GetRecords<GlEntryCsvRecord>().ToList();

            if (records.Count == 0)
                return Results.BadRequest(new { message = "CSV contains no data rows" });

            var trialBalance = new TrialBalance(engagementId, file.FileName);

            var glEntries = records.Select(r => new GLEntry(
                trialBalance.Id,
                engagementId,
                r.AccountCode,
                r.AccountName,
                r.Debit,
                r.Credit
            )).ToList();

            trialBalance.AddGlEntries(glEntries);

            db.TrialBalances.Add(trialBalance);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/engagements/{engagementId}/trial-balance/{trialBalance.Id}",
                new TrialBalanceResponse(
                    trialBalance.Id,
                    engagementId,
                    trialBalance.FileName,
                    trialBalance.SubmittedAt,
                    trialBalance.TotalDebits,
                    trialBalance.TotalCredits,
                    trialBalance.IsBalanced,
                    glEntries.Count));
        })
        .WithName("UploadTrialBalance")
        .Produces<TrialBalanceResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .DisableAntiforgery();
    }
}
