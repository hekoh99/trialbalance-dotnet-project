variable "project_name" {
  type = string
}

variable "environment" {
  type = string
}

variable "location" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "log_analytics_workspace_id" {
  type = string
}

variable "key_vault_uri" {
  type = string
}

variable "servicebus_connection_string" {
  type      = string
  sensitive = true
}

variable "cosmos_connection_string" {
  type      = string
  sensitive = true
}

variable "azure_openai_endpoint" {
  type      = string
  sensitive = true
}

variable "azure_openai_key" {
  type      = string
  sensitive = true
}

variable "azure_openai_deployment" {
  type    = string
  default = "gpt-4o"
}

variable "appinsights_connection_string" {
  type      = string
  sensitive = true
  default   = ""
}

variable "static_web_app_origin" {
  description = "Static Web App URL to allow in CORS (e.g. https://xxx.azurestaticapps.net)"
  type        = string
  default     = ""
}
