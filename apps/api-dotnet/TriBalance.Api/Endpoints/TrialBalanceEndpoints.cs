using TriBalance.Application.Common.Messaging;
using TriBalance.Application.Engagements;
using TriBalance.Application.Engagements.Commands.UploadTrialBalance;

namespace TriBalance.Api.Endpoints;

public static class TrialBalanceEndpoints
{
    public static void MapTrialBalanceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/engagements/{engagementId:guid}").WithTags("Trial Balance");

        group.MapPost("/trial-balance",
            async (Guid engagementId, IFormFile file, ICommandDispatcher dispatcher, CancellationToken ct) =>
            {
                if (file.Length == 0)
                    return Results.BadRequest(new { message = "File is empty" });

                try
                {
                    // Stream is disposed by IFormFile's lifetime; handler consumes synchronously.
                    await using var stream = file.OpenReadStream();
                    var summary = await dispatcher.Send(
                        new UploadTrialBalanceCommand(engagementId, file.FileName, stream), ct);
                    return Results.Created(
                        $"/api/engagements/{engagementId}/trial-balance/{summary.Id}", summary);
                }
                catch (EngagementNotFoundException)
                {
                    return Results.NotFound(new { message = "Engagement not found" });
                }
                catch (InvalidCsvException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            })
        .WithName("UploadTrialBalance")
        .Produces<TrialBalanceSummaryDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .DisableAntiforgery();
    }
}
