using Microsoft.EntityFrameworkCore;
using TriBalance.Api.Endpoints;
using TriBalance.Domain.Engagement;
using TriBalance.Domain.Validation;
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
            .AllowCredentials());
});

builder.Services.AddDbContext<TriBalanceDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<IEngagementRepository, PostgresEngagementRepository>();
builder.Services.AddScoped<IValidationJobRepository, PostgresValidationJobRepository>();

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

app.Run();
