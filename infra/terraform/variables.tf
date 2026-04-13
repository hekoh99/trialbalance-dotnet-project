variable "resource_group_name" {
  description = "기존 리소스 그룹명"
  default     = "compilation-prep-lab-dev-rg"
}

variable "location" {
  description = "Azure 리전"
  default     = "canadacentral"
}

variable "project_name" {
  description = "프로젝트명 (리소스 prefix)"
  default     = "tribalance"
}

variable "environment" {
  description = "환경"
  default     = "dev"
}

variable "postgres_admin_username" {
  description = "PostgreSQL 관리자 계정"
  sensitive   = true
}

variable "postgres_admin_password" {
  description = "PostgreSQL 관리자 비밀번호"
  sensitive   = true
}

variable "postgres_connection_string" {
  description = "PostgreSQL connection string for Key Vault"
  sensitive   = true
  default     = ""
}

variable "azure_openai_location" {
  description = "Azure OpenAI 리전 (Canada East 고정)"
  default     = "canadaeast"
}
