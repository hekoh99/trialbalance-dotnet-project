output "log_analytics_workspace_id" {
  value = azurerm_log_analytics_workspace.law.id
}

output "connection_string" {
  value     = azurerm_application_insights.ai.connection_string
  sensitive = true
}
