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

variable "tenant_id" {
  type = string
}

variable "current_user_object_id" {
  type = string
}

variable "postgres_connection_string" {
  type      = string
  sensitive = true
}

variable "servicebus_connection_string" {
  type      = string
  sensitive = true
}

variable "cosmos_connection_string" {
  type      = string
  sensitive = true
}

variable "azure_openai_key" {
  type      = string
  sensitive = true
}

variable "azure_openai_endpoint" {
  type      = string
  sensitive = true
}
