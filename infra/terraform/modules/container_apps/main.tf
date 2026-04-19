resource "azurerm_container_registry" "acr" {
  name                = "${replace(var.project_name, "-", "")}acr${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = "Basic"
  admin_enabled       = true
}

resource "azurerm_container_app_environment" "env" {
  name                       = "${var.project_name}-cae-${var.environment}"
  location                   = var.location
  resource_group_name        = var.resource_group_name
  log_analytics_workspace_id = var.log_analytics_workspace_id
}

# ── .NET API ──────────────────────────────────────────────────
resource "azurerm_container_app" "api" {
  name                         = "${var.project_name}-api-${var.environment}"
  container_app_environment_id = azurerm_container_app_environment.env.id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  # ACR pull credentials
  registry {
    server               = azurerm_container_registry.acr.login_server
    username             = azurerm_container_registry.acr.admin_username
    password_secret_name = "acr-password"
  }

  secret {
    name  = "acr-password"
    value = azurerm_container_registry.acr.admin_password
  }

  template {
    container {
      name   = "api-dotnet"
      image  = "${azurerm_container_registry.acr.login_server}/api-dotnet:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      # Key Vault URI — .NET reads secrets via AddAzureKeyVault in Program.cs
      env {
        name  = "Azure__KeyVault__Uri"
        value = var.key_vault_uri
      }

      # Postgres (Key Vault will override if secret exists, but this is the fallback)
      env {
        name  = "ConnectionStrings__Postgres"
        value = ""
      }

      # Service Bus
      env {
        name        = "Azure__ServiceBus__ConnectionString"
        secret_name = "sb-conn"
      }

      # Cosmos DB
      env {
        name        = "Azure__CosmosDb__ConnectionString"
        secret_name = "cosmos-conn"
      }

      # Application Insights
      env {
        name  = "ApplicationInsights__ConnectionString"
        value = var.appinsights_connection_string
      }

      # CORS — allow Static Web App origin in production
      env {
        name  = "Cors__AllowedOrigins"
        value = var.static_web_app_origin
      }
    }
  }

  secret {
    name  = "sb-conn"
    value = var.servicebus_connection_string
  }

  secret {
    name  = "cosmos-conn"
    value = var.cosmos_connection_string
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }
}

# ── Python Worker ─────────────────────────────────────────────
resource "azurerm_container_app" "worker" {
  name                         = "${var.project_name}-worker-${var.environment}"
  container_app_environment_id = azurerm_container_app_environment.env.id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"

  registry {
    server               = azurerm_container_registry.acr.login_server
    username             = azurerm_container_registry.acr.admin_username
    password_secret_name = "acr-password"
  }

  secret {
    name  = "acr-password"
    value = azurerm_container_registry.acr.admin_password
  }

  secret {
    name  = "sb-conn"
    value = var.servicebus_connection_string
  }

  secret {
    name  = "cosmos-conn"
    value = var.cosmos_connection_string
  }

  secret {
    name  = "openai-key"
    value = var.azure_openai_key
  }

  template {
    container {
      name   = "worker-python"
      image  = "${azurerm_container_registry.acr.login_server}/worker-python:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name        = "SERVICEBUS_CONNECTION_STRING"
        secret_name = "sb-conn"
      }

      env {
        name        = "COSMOS_CONNECTION_STRING"
        secret_name = "cosmos-conn"
      }

      env {
        name  = "AZURE_OPENAI_ENDPOINT"
        value = var.azure_openai_endpoint
      }

      env {
        name        = "AZURE_OPENAI_KEY"
        secret_name = "openai-key"
      }

      env {
        name  = "AZURE_OPENAI_DEPLOYMENT"
        value = var.azure_openai_deployment
      }

      env {
        name  = "APPLICATIONINSIGHTS_CONNECTION_STRING"
        value = var.appinsights_connection_string
      }
    }

    min_replicas = 1
    max_replicas = 1
  }
  # Worker는 외부 노출 없음 (ingress 없음)
}
