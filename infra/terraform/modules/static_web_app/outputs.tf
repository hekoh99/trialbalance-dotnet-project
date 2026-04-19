output "default_hostname" {
  value = azurerm_static_web_app.web.default_host_name
}

output "api_key" {
  value     = azurerm_static_web_app.web.api_key
  sensitive = true
}
