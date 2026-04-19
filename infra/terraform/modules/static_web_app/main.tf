resource "azurerm_static_web_app" "web" {
  name                = "${var.project_name}-swa-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku_tier            = "Free"
  sku_size            = "Free"
}
