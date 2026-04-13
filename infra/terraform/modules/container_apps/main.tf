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

# resource "azurerm_container_app" "api" {
#   name                         = "${var.project_name}-api-${var.environment}"
#   container_app_environment_id = azurerm_container_app_environment.env.id
#   resource_group_name          = var.resource_group_name
#   revision_mode                = "Single"
#
#   template {
#     container {
#       name   = "api-dotnet"
#       image  = "${azurerm_container_registry.acr.login_server}/api-dotnet:latest"
#       cpu    = 0.25
#       memory = "0.5Gi"
#
#       env {
#         name  = "KeyVault__Uri"
#         value = ""
#       }
#     }
#   }
#
#   ingress {
#     external_enabled = true
#     target_port      = 8080
#     traffic_weight {
#       percentage      = 100
#       latest_revision = true
#     }
#   }
# }

# resource "azurerm_container_app" "worker" {
#   name                         = "${var.project_name}-worker-${var.environment}"
#   container_app_environment_id = azurerm_container_app_environment.env.id
#   resource_group_name          = var.resource_group_name
#   revision_mode                = "Single"
#
#   template {
#     container {
#       name   = "worker-python"
#       image  = "${azurerm_container_registry.acr.login_server}/worker-python:latest"
#       cpu    = 0.25
#       memory = "0.5Gi"
#     }
#
#     min_replicas = 1
#     max_replicas = 1
#   }
# }
