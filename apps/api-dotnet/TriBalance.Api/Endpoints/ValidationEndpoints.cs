using TriBalance.Application.Common.Messaging;
using TriBalance.Application.Engagements;
using TriBalance.Application.Validation;
using TriBalance.Application.Validation.Commands.TriggerValidation;
using TriBalance.Application.Validation.Queries.GetValidationResult;
using TriBalance.Application.Validation.Queries.GetValidationStatus;

namespace TriBalance.Api.Endpoints;

public static class ValidationEndpoints
{
    public record TriggerValidationRequest(Guid TrialBalanceId);

    public static void MapValidationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/engagements/{engagementId:guid}").WithTags("Validation");

        group.MapGet("/status",
            async (Guid engagementId, IQueryDispatcher dispatcher, CancellationToken ct) =>
            {
                var dto = await dispatcher.Send(new GetValidationStatusQuery(engagementId), ct);
                return dto is null
                    ? Results.Ok(new { status = "none", message = "No validation jobs found" })
                    : Results.Ok(dto);
            })
        .WithName("GetValidationStatus")
        .Produces<ValidationJobDto>();

        // Trigger validation. Creates a Queued job, publishes to Service Bus, pushes Queued
        // to SignalR. Each call creates a new job, satisfying Scenario 3 retry.
        group.MapPost("/validate",
            async (
                Guid engagementId,
                TriggerValidationRequest request,
                ICommandDispatcher dispatcher,
                CancellationToken ct) =>
            {
                try
                {
                    var dto = await dispatcher.Send(
                        new TriggerValidationCommand(engagementId, request.TrialBalanceId), ct);
                    return Results.Accepted($"/api/engagements/{engagementId}/status", dto);
                }
                catch (TrialBalanceNotFoundException)
                {
                    return Results.NotFound(new { message = "Trial balance not found for engagement" });
                }
                catch (InvalidOperationException ex)
                {
                    // DisabledValidationRequestPublisher throws this when Service Bus isn't configured.
                    return Results.Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
                }
            })
        .WithName("TriggerValidation")
        .Produces<ValidationJobDto>(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status503ServiceUnavailable);

        // Latest classification result — Angular dashboard calls this once SignalR flips status to Completed.
        group.MapGet("/validation",
            async (Guid engagementId, IQueryDispatcher dispatcher, CancellationToken ct) =>
            {
                var dto = await dispatcher.Send(new GetValidationResultQuery(engagementId), ct);
                return dto is null
                    ? Results.NotFound(new { message = "No validation results yet" })
                    : Results.Ok(dto);
            })
        .WithName("GetValidationResult")
        .Produces<ValidationResultDto>()
        .Produces(StatusCodes.Status404NotFound);
    }
}
