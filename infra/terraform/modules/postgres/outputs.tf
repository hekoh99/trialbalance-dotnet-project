output "server_fqdn" {
  value = data.azurerm_postgresql_flexible_server.postgres.fqdn
}

output "database_name" {
  value = azurerm_postgresql_flexible_server_database.tribalance_db.name
}
