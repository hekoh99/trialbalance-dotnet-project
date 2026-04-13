output "endpoint" {
  value = azurerm_cosmosdb_account.cosmos.endpoint
}

output "primary_connection_string" {
  value     = azurerm_cosmosdb_account.cosmos.primary_sql_connection_string
  sensitive = true
}
