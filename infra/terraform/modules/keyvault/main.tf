resource "azurerm_key_vault" "kv" {
  name                = "${var.project_name}-kv-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name
  tenant_id           = var.tenant_id
  sku_name            = "standard"

  access_policy {
    tenant_id = var.tenant_id
    object_id = var.current_user_object_id

    secret_permissions = [
      "Get", "List", "Set", "Delete", "Purge"
    ]
  }

  soft_delete_retention_days = 7
}

# Secret names use '--' as separator because Key Vault doesn't allow ':'.
# .NET's AddAzureKeyVault provider automatically translates '--' → ':' on load.
# e.g. "ConnectionStrings--Postgres" → config key "ConnectionStrings:Postgres"

resource "azurerm_key_vault_secret" "postgres_connection_string" {
  name         = "ConnectionStrings--Postgres"
  value        = var.postgres_connection_string
  key_vault_id = azurerm_key_vault.kv.id
}

resource "azurerm_key_vault_secret" "servicebus_connection_string" {
  name         = "Azure--ServiceBus--ConnectionString"
  value        = var.servicebus_connection_string
  key_vault_id = azurerm_key_vault.kv.id
}

resource "azurerm_key_vault_secret" "cosmos_connection_string" {
  name         = "Azure--CosmosDb--ConnectionString"
  value        = var.cosmos_connection_string
  key_vault_id = azurerm_key_vault.kv.id
}

resource "azurerm_key_vault_secret" "azure_openai_key" {
  name         = "Azure--OpenAI--ApiKey"
  value        = var.azure_openai_key
  key_vault_id = azurerm_key_vault.kv.id
}

resource "azurerm_key_vault_secret" "azure_openai_endpoint" {
  name         = "Azure--OpenAI--Endpoint"
  value        = var.azure_openai_endpoint
  key_vault_id = azurerm_key_vault.kv.id
}

resource "azurerm_key_vault_secret" "appinsights_connection_string" {
  name         = "ApplicationInsights--ConnectionString"
  value        = var.appinsights_connection_string
  key_vault_id = azurerm_key_vault.kv.id
}
