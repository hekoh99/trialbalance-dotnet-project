output "cosmos_endpoint" {
  value = module.cosmos.endpoint
}

output "servicebus_namespace_name" {
  value = module.servicebus.namespace_name
}

# OpenAI managed separately — endpoint/key passed via variables
# output "openai_endpoint" {
#   value     = module.openai.endpoint
#   sensitive = true
# }

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

output "static_web_app_hostname" {
  value = module.static_web_app.default_hostname
}

output "static_web_app_api_key" {
  value     = module.static_web_app.api_key
  sensitive = true
}
