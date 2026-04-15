using TriBalance.Application.Common.Messaging;
using TriBalance.Application.Engagements;
using TriBalance.Application.Engagements.Commands.CreateEngagement;
using TriBalance.Application.Engagements.Queries.GetEngagement;
using TriBalance.Application.Engagements.Queries.ListEngagements;

namespace TriBalance.Api.Endpoints;

public static class EngagementEndpoints
{
    public record CreateEngagementRequest(string ClientName, DateTime FiscalYearEnd);

    public static void MapEngagementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/engagements").WithTags("Engagements");

        group.MapPost("/",
            async (CreateEngagementRequest request, ICommandDispatcher dispatcher, CancellationToken ct) =>
            {
                var dto = await dispatcher.Send(
                    new CreateEngagementCommand(request.ClientName, request.FiscalYearEnd), ct);
                return Results.Created($"/api/engagements/{dto.Id}", dto);
            })
        .WithName("CreateEngagement")
        .Produces<EngagementDto>(StatusCodes.Status201Created);

        group.MapGet("/{id:guid}",
            async (Guid id, IQueryDispatcher dispatcher, CancellationToken ct) =>
            {
                var dto = await dispatcher.Send(new GetEngagementQuery(id), ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            })
        .WithName("GetEngagement")
        .Produces<EngagementDto>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/",
            async (IQueryDispatcher dispatcher, CancellationToken ct) =>
                Results.Ok(await dispatcher.Send(new ListEngagementsQuery(), ct)))
        .WithName("ListEngagements")
        .Produces<IReadOnlyList<EngagementDto>>();
    }
}
