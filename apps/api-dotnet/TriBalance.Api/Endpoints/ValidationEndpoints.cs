using Microsoft.EntityFrameworkCore;
using TriBalance.Api.Hubs;
using TriBalance.Domain.Validation;
using TriBalance.Infrastructure.Messaging;
using TriBalance.Infrastructure.Persistence.CosmosDB;
using TriBalance.Infrastructure.Persistence.PostgreSQL;

namespace TriBalance.Api.Endpoints;

public static class ValidationEndpoints
{
    public record ValidationJobResponse(
        Guid Id,
        Guid EngagementId,
        Guid TrialBalanceId,
        string Status,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime? CompletedAt,
        string? ErrorMessage);

    public record TriggerValidationRequest(Guid TrialBalanceId);

    public static void MapValidationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/engagements/{engagementId:guid}").WithTags("Validation");

        // Latest job status (used by Angular dashboard on load before SignalR delivers events).
        group.MapGet("/status", async (Guid engagementId, IValidationJobRepository repo) =>
        {
            var jobs = await repo.GetByEngagementIdAsync(engagementId);
            var latest = jobs.FirstOrDefault();

            if (latest is null)
                return Results.Ok(new { status = "none", message = "No validation jobs found" });

            return Results.Ok(ToResponse(latest));
        })
        .WithName("GetValidationStatus")
        .Produces<ValidationJobResponse>();

        // Trigger validation. Creates a Queued job, publishes to Service Bus, pushes Queued
        // to SignalR. Idempotent w.r.t. retry: each call creates a new job for the same trial
        // balance, which satisfies Scenario 3's retry requirement.
        group.MapPost("/validate",
            async (
                Guid engagementId,
                TriggerValidationRequest request,
                TriBalanceDbContext db,
                IValidationJobRepository jobRepo,
                IValidationRequestPublisher publisher,
                IValidationStatusNotifier notifier,
                CancellationToken ct) =>
            {
                var tb = await db.TrialBalances
                    .Include(t => t.GlEntries)
                    .FirstOrDefaultAsync(
                        t => t.Id == request.TrialBalanceId && t.EngagementId == engagementId,
                        ct);

                if (tb is null)
                    return Results.NotFound(new { message = "Trial balance not found for engagement" });
                if (tb.GlEntries.Count == 0)
                    return Results.BadRequest(new { message = "Trial balance has no GL entries" });

                var job = new ValidationJob(engagementId, tb.Id);
                await jobRepo.AddAsync(job);

                var message = new ValidationRequestMessage(
                    engagementId,
                    tb.Id,
                    job.Id,
                    tb.GlEntries.Select(g => new GlEntryMessage(
                        g.AccountCode,
                        g.AccountName,
                        g.Debit,
                        g.Credit,
                        g.Balance)).ToList());

                try
                {
                    await publisher.PublishAsync(message, ct);
                }
                catch (InvalidOperationException ex)
                {
                    // DisabledValidationRequestPublisher throws this when Service Bus isn't configured.
                    return Results.Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
                }
                await notifier.PushAsync(engagementId, job.Id, "Queued", cancellationToken: ct);

                return Results.Accepted($"/api/engagements/{engagementId}/status", ToResponse(job));
            })
        .WithName("TriggerValidation")
        .Produces<ValidationJobResponse>(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // Latest classification result from Cosmos. Angular dashboard calls this
        // once the SignalR status flips to Completed.
        group.MapGet("/validation",
            async (Guid engagementId, IValidationResultRepository repo, CancellationToken ct) =>
            {
                var doc = await repo.GetLatestByEngagementAsync(engagementId, ct);
                if (doc is null)
                    return Results.NotFound(new { message = "No validation results yet" });
                return Results.Ok(doc);
            })
        .WithName("GetValidationResult")
        .Produces<ClassificationResultDocument>()
        .Produces(StatusCodes.Status404NotFound);
    }

    private static ValidationJobResponse ToResponse(ValidationJob j) => new(
        j.Id,
        j.EngagementId,
        j.TrialBalanceId,
        j.Status.ToString(),
        j.CreatedAt,
        j.UpdatedAt,
        j.CompletedAt,
        j.ErrorMessage);
}
