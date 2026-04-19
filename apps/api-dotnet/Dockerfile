# ── Stage 1: restore + build ────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1a. Copy csproj + sln first for layer-cached restore
COPY TriBalance.sln .
COPY TriBalance.Domain/TriBalance.Domain.csproj          TriBalance.Domain/
COPY TriBalance.Application/TriBalance.Application.csproj TriBalance.Application/
COPY TriBalance.Infrastructure/TriBalance.Infrastructure.csproj TriBalance.Infrastructure/
COPY TriBalance.Api/TriBalance.Api.csproj                TriBalance.Api/
RUN dotnet restore TriBalance.sln

# 1b. Copy everything and publish release build
COPY . .
RUN dotnet publish TriBalance.Api/TriBalance.Api.csproj \
    -c Release -o /app/publish

# ── Stage 2: runtime-only image ────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user for security (Container Apps best practice)
RUN adduser --disabled-password --no-create-home appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TriBalance.Api.dll"]
