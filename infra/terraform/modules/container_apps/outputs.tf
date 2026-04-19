output "acr_login_server" {
  value = azurerm_container_registry.acr.login_server
}

output "api_url" {
  value = azurerm_container_app.api.latest_revision_fqdn
}

output "api_identity_principal_id" {
  value = azurerm_container_app.api.identity[0].principal_id
}
