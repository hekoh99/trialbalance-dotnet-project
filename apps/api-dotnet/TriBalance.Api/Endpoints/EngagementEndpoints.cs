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

        group.MapGet("/{id:guid}", // if  (not-a-guid) → 400 (.NET 자동 처리)
            async (Guid id, IQueryDispatcher dispatcher, CancellationToken ct) =>
            {
                var dto = await dispatcher.Send(new GetEngagementQuery(id), ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto); // 만약 id에 해당하는 Engagement가 없으면 404 Not Found 반환, 있으면 200 OK와 함께 DTO 반환
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
