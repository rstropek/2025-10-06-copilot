# Product List

## Introduction

We need a list of products for our end users. This is a sample application that acts as a prototype. For demonstration purposes, we have sample product data in `/WebApi/Products/hagleitner-products.json`. Use that JSON file for now. Later, we will replace it with a database.

For now, we do not need any filtering functionality. Just return the full list of products.

We need a web API in the `WebApi` project. The UI must be in the Angular project `WebUI`.

## Implementation Notes

### Web API

* Please create a dedicated module similar to `\WebApi\Healthchecks\HealthcheckRoutes.cs` for product-related routes.
* Create a web API `GET /products` to return the list of products.
* Create dedicated C# records as DTOs. Do NOT use these DTOs for deserialization of the JSON file. Create separate records for deserialization. Use C# PascalCase naming convention for the C# records. ASP.NET core will automatically convert the property names to camelCase in the JSON response.
* Use `System.Text.Json` for JSON serialization and deserialization.
* On top level, use a JSON array for the list of products. Do NOT wrap the list in an object.

### Web UI

* Create a dedicated route for the product list: `/products`
* Create a component for the product list: `ProductList` (do NOT use `ProductListComponent` as name)
* Note that we have Angular Bootstrap installed. Use Bootstrap classes/components for styling.
* Display the products as cards in a grid layout.
* Do not forget to add a link to the product list in the navigation bar (`/WebUI/src/app/main-menu`)

### Testing

For now, we do not need automated tests. We will add them later.
