output "cosmos_endpoint" {
  value = module.cosmos.endpoint
}

output "servicebus_namespace_name" {
  value = module.servicebus.namespace_name
}

output "openai_endpoint" {
  value     = module.openai.endpoint
  sensitive = true
}

output "key_vault_uri" {
  value = module.keyvault.vault_uri
}

output "application_insights_connection_string" {
  value     = module.observability.connection_string
  sensitive = true
}

output "container_registry_login_server" {
  value = module.container_apps.acr_login_server
}
