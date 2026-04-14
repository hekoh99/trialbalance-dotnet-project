using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using TriBalance.Api.Endpoints;
using TriBalance.Api.Hubs;
using TriBalance.Domain.Engagement;
using TriBalance.Domain.Validation;
using TriBalance.Infrastructure.Messaging;
using TriBalance.Infrastructure.Persistence.CosmosDB;
using TriBalance.Infrastructure.Persistence.PostgreSQL;
using TriBalance.Infrastructure.Persistence.PostgreSQL.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()); // required for SignalR WebSocket auth
});

builder.Services.AddDbContext<TriBalanceDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<IEngagementRepository, PostgresEngagementRepository>();
builder.Services.AddScoped<IValidationJobRepository, PostgresValidationJobRepository>();

// SignalR notifier + adapter (always registered; hub lives in-process)
builder.Services.AddSingleton<IValidationStatusNotifier, SignalRValidationStatusNotifier>();
builder.Services.AddSingleton<IValidationStatusNotifierAdapter, SignalRValidationStatusNotifierAdapter>();

// --- Service Bus (request publisher + result consumer) ---
// Conditionally registered: allows the API to start locally for Day 1-2 features
// when Azure isn't fully configured yet. /validate returns 503 in that case.
var serviceBusOptions = new ServiceBusOptions();
builder.Configuration.GetSection("Azure:ServiceBus").Bind(serviceBusOptions);
builder.Services.AddSingleton(serviceBusOptions);

if (!string.IsNullOrWhiteSpace(serviceBusOptions.ConnectionString))
{
    builder.Services.AddSingleton(_ => new ServiceBusClient(serviceBusOptions.ConnectionString));
    builder.Services.AddSingleton<IValidationRequestPublisher, ServiceBusValidationRequestPublisher>();
    builder.Services.AddHostedService<ValidationResultConsumer>();
}
else
{
    builder.Services.AddSingleton<IValidationRequestPublisher, DisabledValidationRequestPublisher>();
}

// --- Cosmos DB (validation result reads) ---
var cosmosOptions = new CosmosOptions();
builder.Configuration.GetSection("Azure:CosmosDb").Bind(cosmosOptions);
builder.Services.AddSingleton(cosmosOptions);

if (!string.IsNullOrWhiteSpace(cosmosOptions.ConnectionString))
{
    builder.Services.AddSingleton(_ => new CosmosClient(cosmosOptions.ConnectionString));
    builder.Services.AddScoped<IValidationResultRepository, CosmosValidationResultRepository>();
}
else
{
    builder.Services.AddScoped<IValidationResultRepository, DisabledValidationResultRepository>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Angular");

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("HealthCheck");

app.MapEngagementEndpoints();
app.MapTrialBalanceEndpoints();
app.MapValidationEndpoints();

app.MapHub<ValidationHub>("/hubs/validation");

app.Run();
