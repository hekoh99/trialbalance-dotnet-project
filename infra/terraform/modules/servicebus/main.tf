resource "azurerm_servicebus_namespace" "sb" {
  name                = "${var.project_name}-sb-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "Standard"
}

resource "azurerm_servicebus_queue" "validation_request" {
  name         = "tb-validation-request"
  namespace_id = azurerm_servicebus_namespace.sb.id

  dead_lettering_on_message_expiration = true
  max_delivery_count                   = 3
  default_message_ttl                  = "PT1H"
}

resource "azurerm_servicebus_queue" "validation_result" {
  name         = "tb-validation-result"
  namespace_id = azurerm_servicebus_namespace.sb.id

  dead_lettering_on_message_expiration = true
  max_delivery_count                   = 3
  default_message_ttl                  = "PT1H"
}
