# TriBalance

Trial Balance validation and account auto-classification service.

## Architecture

- **api-dotnet** — .NET 8 Minimal API (DDD, EF Core, PostgreSQL, SignalR)
- **worker-python** — Python AI Worker (Azure OpenAI, Service Bus, Cosmos DB)
- **web-angular** — Angular 16+ frontend (Material, SignalR)
- **infra/terraform** — All Azure resources as IaC

## Quick Start

```bash
# .NET API
cd apps/api-dotnet
dotnet run --project TriBalance.Api

# Python Worker
cd apps/worker-python
python -m uvicorn app.main:app --port 8000

# Angular
cd apps/web-angular
ng serve
```

## Azure Resources

All infrastructure is managed via Terraform in `infra/terraform/`.

```bash
cd infra/terraform
terraform init
terraform plan
terraform apply
```
