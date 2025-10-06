using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApi.Products;

public static class ProductRoutes
{
    extension(IEndpointRouteBuilder api)
    {
        public IEndpointRouteBuilder MapProductEndpoints()
        {
            var productsApi = api.MapGroup("/products");

            productsApi.MapGet("/", async (string? category) =>
            {
                var jsonPath = Path.Combine(AppContext.BaseDirectory, "Products", "hagleitner-products.json");
                var jsonContent = await File.ReadAllTextAsync(jsonPath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var productsWrapper = JsonSerializer.Deserialize<ProductsWrapper>(jsonContent, options);

                if (productsWrapper?.Products == null)
                {
                    return Results.Problem("Failed to load products");
                }

                var products = productsWrapper.Products;
                
                // Apply optional category filter
                if (!string.IsNullOrWhiteSpace(category))
                {
                    products = products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var productDtos = products.Select(p => new ProductDto(
                    p.ProductId,
                    p.ArticleNumber,
                    p.ArticleName,
                    p.Description,
                    p.Category,
                    p.Tags
                )).ToList();

                return Results.Ok(productDtos);
            });

            productsApi.MapGet("/categories", async () =>
            {
                var jsonPath = Path.Combine(AppContext.BaseDirectory, "Products", "hagleitner-products.json");
                var jsonContent = await File.ReadAllTextAsync(jsonPath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var productsWrapper = JsonSerializer.Deserialize<ProductsWrapper>(jsonContent, options);

                if (productsWrapper?.Products == null)
                {
                    return Results.Problem("Failed to load products");
                }

                var categories = productsWrapper.Products
                    .Select(p => p.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                return Results.Ok(categories);
            });

            return api;
        }
    }
}

// Internal record for deserializing the JSON file
internal record ProductsWrapper(
    [property: JsonPropertyName("products")] List<ProductJson> Products
);

internal record ProductJson(
    [property: JsonPropertyName("productID")] int ProductId,
    [property: JsonPropertyName("articleNumber")] string ArticleNumber,
    [property: JsonPropertyName("articleName")] string ArticleName,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("tags")] List<string> Tags
);

// Public DTO for the API response
public record ProductDto(
    int ProductId,
    string ArticleNumber,
    string ArticleName,
    string Description,
    string Category,
    List<string> Tags
);
