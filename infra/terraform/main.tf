terraform {
  required_version = ">= 1.7.0"
}

provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "uvse" {
  name     = "rg-uvse-prod"
  location = "East US"
}

resource "azurerm_virtual_network" "uvse" {
  name                = "vnet-uvse-prod"
  location            = azurerm_resource_group.uvse.location
  resource_group_name = azurerm_resource_group.uvse.name
  address_space       = ["10.42.0.0/16"]
}

resource "azurerm_web_application_firewall_policy" "uvse" {
  name                = "waf-uvse-prod"
  resource_group_name = azurerm_resource_group.uvse.name
  location            = azurerm_resource_group.uvse.location
}
