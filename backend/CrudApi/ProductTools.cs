using System.ComponentModel;
using CrudApi.Dtos;
using CrudApi.Mcp.Contracts;
using CrudApi.Services;
using ModelContextProtocol.Server;

namespace CrudApi.Mcp.Tools;

[McpServerToolType]
public sealed class ProductTools(IProductService productService)
{
    [McpServerTool(Name = "health_check", Title = "Health Check", ReadOnly = true, UseStructuredContent = true)]
    [Description("Checks whether the underlying CrudApi service is reachable and healthy.")]
    public HealthCheckResult HealthCheck() => new(true, "ok", "CrudApi");

    [McpServerTool(Name = "products_list", Title = "List Products", ReadOnly = true, UseStructuredContent = true)]
    [Description("Returns all products from the CrudApi backend.")]
    public async Task<ProductListResult> ListProducts(CancellationToken cancellationToken)
    {
        var products = await productService.GetProductsAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return new ProductListResult(true, products);
    }

    [McpServerTool(Name = "products_get", Title = "Get Product", ReadOnly = true, UseStructuredContent = true)]
    [Description("Returns a single product by numeric id.")]
    public async Task<ProductItemResult> GetProduct(
        [Description("The product id.")] int id,
        CancellationToken cancellationToken)
    {
        var product = await productService.GetProductAsync(id);
        cancellationToken.ThrowIfCancellationRequested();

        return product is null
            ? new ProductItemResult(false, Error: new ErrorDetails("not_found", $"Product {id} was not found."))
            : new ProductItemResult(true, product);
    }

    [McpServerTool(Name = "products_create", Title = "Create Product", UseStructuredContent = true)]
    [Description("Creates a product in the CrudApi backend.")]
    public async Task<ProductItemResult> CreateProduct(
        [Description("The product name. Required, non-whitespace, max 120 characters.")] string name,
        [Description("Optional description, max 1000 characters.")] string? description,
        [Description("Price between 0 and 999999999.")] decimal price,
        [Description("Stock quantity, minimum 0.")] int stock,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateProductInput(name, description, price, stock);
        if (validationErrors.Count > 0)
        {
            return new ProductItemResult(false, Error: new ErrorDetails(
                "validation_error",
                "The request payload is invalid.",
                validationErrors));
        }

        var request = new CreateProductRequest(name, description, price, stock);
        var product = await productService.CreateProductAsync(request);
        cancellationToken.ThrowIfCancellationRequested();
        return new ProductItemResult(true, product);
    }

    [McpServerTool(Name = "products_update", Title = "Update Product", UseStructuredContent = true)]
    [Description("Updates an existing product in the CrudApi backend.")]
    public async Task<ProductItemResult> UpdateProduct(
        [Description("The product id.")] int id,
        [Description("The product name. Required, non-whitespace, max 120 characters.")] string name,
        [Description("Optional description, max 1000 characters.")] string? description,
        [Description("Price between 0 and 999999999.")] decimal price,
        [Description("Stock quantity, minimum 0.")] int stock,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateProductInput(name, description, price, stock);
        if (validationErrors.Count > 0)
        {
            return new ProductItemResult(false, Error: new ErrorDetails(
                "validation_error",
                "The request payload is invalid.",
                validationErrors));
        }

        var request = new UpdateProductRequest(name, description, price, stock);
        var product = await productService.UpdateProductAsync(id, request);
        cancellationToken.ThrowIfCancellationRequested();

        return product is null
            ? new ProductItemResult(false, Error: new ErrorDetails("not_found", $"Product {id} was not found."))
            : new ProductItemResult(true, product);
    }

    [McpServerTool(Name = "products_delete", Title = "Delete Product", UseStructuredContent = true)]
    [Description("Deletes a product by numeric id.")]
    public async Task<DeleteProductResult> DeleteProduct(
        [Description("The product id.")] int id,
        CancellationToken cancellationToken)
    {
        var deleted = await productService.DeleteProductAsync(id);
        cancellationToken.ThrowIfCancellationRequested();

        return deleted
            ? new DeleteProductResult(true, true, id)
            : new DeleteProductResult(false, false, id, new ErrorDetails("not_found", $"Product {id} was not found."));
    }

    private static Dictionary<string, string[]> ValidateProductInput(string name, string? description, decimal price, int stock)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(name) || name.Length > 120)
        {
            Add("Name", "Name is required and must be 120 characters or fewer.");
        }

        if (description is { Length: > 1000 })
        {
            Add("Description", "The field Description must be a string with a maximum length of 1000.");
        }

        if (price is < 0 or > 999_999_999)
        {
            Add("Price", "The field Price must be between 0 and 999999999.");
        }

        if (stock < 0)
        {
            Add("Stock", "The field Stock must be between 0 and 2147483647.");
        }

        return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());

        void Add(string key, string message)
        {
            if (!errors.TryGetValue(key, out var messages))
            {
                messages = [];
                errors[key] = messages;
            }

            messages.Add(message);
        }
    }
}
