using Microsoft.EntityFrameworkCore;
using TriBalance.Domain.Engagement;
using TriBalance.Infrastructure.Persistence.PostgreSQL;

namespace TriBalance.Api.Endpoints;

public static class EngagementEndpoints
{
    public record CreateEngagementRequest(string ClientName, DateTime FiscalYearEnd);

    public record EngagementResponse(
        Guid Id,
        string ClientName,
        DateTime FiscalYearEnd,
        DateTime CreatedAt,
        List<TrialBalanceSummary> TrialBalances);

    public record TrialBalanceSummary(
        Guid Id,
        string FileName,
        DateTime SubmittedAt,
        decimal TotalDebits,
        decimal TotalCredits,
        bool IsBalanced,
        int GlEntryCount);

    public static void MapEngagementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/engagements").WithTags("Engagements");

        group.MapPost("/", async (CreateEngagementRequest request, IEngagementRepository repo) =>
        {
            var engagement = new Engagement(request.ClientName, request.FiscalYearEnd);
            await repo.AddAsync(engagement);
            return Results.Created($"/api/engagements/{engagement.Id}", ToResponse(engagement));
        })
        .WithName("CreateEngagement")
        .Produces<EngagementResponse>(StatusCodes.Status201Created);

        group.MapGet("/{id:guid}", async (Guid id, IEngagementRepository repo) =>
        {
            var engagement = await repo.GetByIdAsync(id);
            if (engagement is null)
                return Results.NotFound();
            return Results.Ok(ToResponse(engagement));
        })
        .WithName("GetEngagement")
        .Produces<EngagementResponse>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/", async (IEngagementRepository repo) =>
        {
            var engagements = await repo.GetAllAsync();
            return Results.Ok(engagements.Select(ToResponse));
        })
        .WithName("ListEngagements")
        .Produces<IEnumerable<EngagementResponse>>();
    }

    private static EngagementResponse ToResponse(Engagement e) => new(
        e.Id,
        e.ClientName,
        e.FiscalYearEnd,
        e.CreatedAt,
        e.TrialBalances.Select(tb => new TrialBalanceSummary(
            tb.Id,
            tb.FileName,
            tb.SubmittedAt,
            tb.TotalDebits,
            tb.TotalCredits,
            tb.IsBalanced,
            tb.GlEntries.Count
        )).ToList()
    );
}
