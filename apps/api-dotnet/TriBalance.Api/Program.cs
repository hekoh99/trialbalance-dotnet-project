using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using TriBalance.Api.Endpoints;
using TriBalance.Api.Hubs;
using TriBalance.Application.Common.Behaviors;
using TriBalance.Application.Common.Messaging;
using TriBalance.Application.Engagements;
using TriBalance.Application.Validation;
using TriBalance.Domain.Engagement;
using TriBalance.Domain.Validation;
using TriBalance.Infrastructure.Messaging;
using TriBalance.Infrastructure.Persistence.CosmosDB;
using TriBalance.Infrastructure.Persistence.PostgreSQL;
using TriBalance.Infrastructure.Persistence.PostgreSQL.CsvParsing;
using TriBalance.Infrastructure.Persistence.PostgreSQL.Repositories;

var builder = WebApplication.CreateBuilder(args);

// --- Key Vault configuration provider ---
// When Azure:KeyVault:Uri is set, Key Vault secrets are merged into IConfiguration
// BEFORE service registration — Bind() calls below pick them up transparently.
// Secret naming: Key Vault doesn't allow ':', use '--' instead.
//   "Azure--ServiceBus--ConnectionString" → config "Azure:ServiceBus:ConnectionString".
// DefaultAzureCredential: env vars → managed identity → VS / az-cli login.
var keyVaultUri = builder.Configuration["Azure:KeyVault:Uri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}

// --- Application Insights ---
// Automatic request/dependency tracking + ILogger scrape (including LoggingBehavior
// emits like "{CommandName} handled in {Elapsed}ms"). Connection string comes from
// config → Key Vault overrides in production.
// Only wire when a connection string is present — AddApplicationInsightsTelemetry
// registers a metric exporter that fails at DI resolution if the connection string
// is empty, so skipping the call entirely is the right guard for local/dev.
var aiConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(aiConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
        options.ConnectionString = aiConnectionString);
}

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

// --- Application messaging (Commands/Queries + dispatchers) ---
builder.Services.AddApplicationMessaging();
builder.Services.AddPipelineBehavior(typeof(LoggingBehavior<,>));

// --- Persistence ---
builder.Services.AddDbContext<TriBalanceDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<IEngagementRepository, PostgresEngagementRepository>();
builder.Services.AddScoped<ITrialBalanceRepository, PostgresTrialBalanceRepository>();
builder.Services.AddScoped<IValidationJobRepository, PostgresValidationJobRepository>();
builder.Services.AddSingleton<IGlEntryCsvParser, CsvHelperGlEntryParser>();

// SignalR notifier implements the Application port directly — no adapter shim needed.
builder.Services.AddSingleton<IValidationStatusNotifier, SignalRValidationStatusNotifier>();

// --- Service Bus (request publisher + result consumer) ---
// Conditionally registered so the API still starts locally for dev without Azure.
// /validate returns 503 in that case (DisabledValidationRequestPublisher).
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
    builder.Services.AddScoped<IValidationResultReader, CosmosValidationResultRepository>();
}
else
{
    builder.Services.AddScoped<IValidationResultReader, DisabledValidationResultRepository>();
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
