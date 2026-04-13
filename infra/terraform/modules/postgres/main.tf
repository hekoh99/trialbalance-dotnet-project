data "azurerm_postgresql_flexible_server" "postgres" {
  name                = "${var.project_name}-pg-${var.environment}"
  resource_group_name = var.resource_group_name
}

resource "azurerm_postgresql_flexible_server_database" "tribalance_db" {
  name      = "tribalance"
  server_id = data.azurerm_postgresql_flexible_server.postgres.id
  collation = "en_US.utf8"
  charset   = "utf8"
}
