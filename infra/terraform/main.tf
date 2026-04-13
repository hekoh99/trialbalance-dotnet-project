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
  azure_openai_key            = module.openai.primary_key
}

module "openai" {
  source              = "./modules/openai"
  project_name        = var.project_name
  environment         = var.environment
  resource_group_name = data.azurerm_resource_group.rg.name
}

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
}
