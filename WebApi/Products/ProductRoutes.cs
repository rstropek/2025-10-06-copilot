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

            productsApi.MapGet("/", async () =>
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

                var productDtos = productsWrapper.Products.Select(p => new ProductDto(
                    p.ProductId,
                    p.ArticleNumber,
                    p.ArticleName,
                    p.Description,
                    p.Category,
                    p.Tags
                )).ToList();

                return Results.Ok(productDtos);
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
