terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy = true
    }
  }
}

data "azurerm_resource_group" "rg" {
  name = var.resource_group_name
}

data "azurerm_client_config" "current" {}

module "cosmos" {
  source              = "./modules/cosmos"
  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = data.azurerm_resource_group.rg.name
}

module "servicebus" {
  source              = "./modules/servicebus"
  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = data.azurerm_resource_group.rg.name
}

module "keyvault" {
  source                      = "./modules/keyvault"
  project_name                = var.project_name
  environment                 = var.environment
  location                    = var.location
  resource_group_name         = data.azurerm_resource_group.rg.name
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  current_user_object_id      = data.azurerm_client_config.current.object_id
  postgres_connection_string  = var.postgres_connection_string
  servicebus_connection_string = module.servicebus.primary_connection_string
  cosmos_connection_string    = module.cosmos.primary_connection_string
  azure_openai_key            = var.azure_openai_key
  azure_openai_endpoint       = var.azure_openai_endpoint
  appinsights_connection_string = module.observability.connection_string
}

# Using a separate OpenAI resource — module disabled.
# module "openai" {
#   source              = "./modules/openai"
#   project_name        = var.project_name
#   environment         = var.environment
#   resource_group_name = data.azurerm_resource_group.rg.name
# }

module "observability" {
  source              = "./modules/observability"
  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = data.azurerm_resource_group.rg.name
}

module "container_apps" {
  source                     = "./modules/container_apps"
  project_name               = var.project_name
  environment                = var.environment
  location                   = var.location
  resource_group_name        = data.azurerm_resource_group.rg.name
  log_analytics_workspace_id = module.observability.log_analytics_workspace_id
  key_vault_uri              = module.keyvault.vault_uri
  servicebus_connection_string = module.servicebus.primary_connection_string
  cosmos_connection_string   = module.cosmos.primary_connection_string
  azure_openai_endpoint      = var.azure_openai_endpoint
  azure_openai_key           = var.azure_openai_key
  appinsights_connection_string = module.observability.connection_string
  static_web_app_origin        = "https://${module.static_web_app.default_hostname}"
}

# ── Key Vault access policy for API's Managed Identity ────────
# Separate resource (not inline in keyvault module) to avoid circular
# dependency: keyvault creates secrets → container_apps references them
# → container_apps creates identity → identity needs keyvault access.
# azurerm_key_vault_access_policy runs after both modules are done.
resource "azurerm_key_vault_access_policy" "api_identity" {
  key_vault_id = module.keyvault.vault_id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = module.container_apps.api_identity_principal_id

  secret_permissions = ["Get", "List"]
}

# ── Angular frontend ─────────────────────────────────────────
module "static_web_app" {
  source              = "./modules/static_web_app"
  project_name        = var.project_name
  environment         = var.environment
  resource_group_name = data.azurerm_resource_group.rg.name
}
