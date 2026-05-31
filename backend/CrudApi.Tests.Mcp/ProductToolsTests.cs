using CrudApi.Dtos;
using CrudApi.Mcp.Resources;
using CrudApi.Mcp.Tools;
using CrudApi.Services;

namespace CrudApi.Mcp.Tests;

[TestClass]
public sealed class ProductToolsTests
{
    [TestMethod]
    public async Task GetProduct_WhenServiceReturnsNotFound_ReturnsStructuredError()
    {
        var tools = new ProductTools(new FakeProductService());

        var result = await tools.GetProduct(123, CancellationToken.None);

        Assert.IsFalse(result.Success);
        Assert.IsNull(result.Product);
        Assert.IsNotNull(result.Error);
        Assert.AreEqual("not_found", result.Error.Code);
        Assert.AreEqual("Product 123 was not found.", result.Error.Message);
    }

    [TestMethod]
    public async Task CreateProduct_WhenValidationFails_ReturnsValidationErrorDetails()
    {
        var tools = new ProductTools(new FakeProductService());

        var result = await tools.CreateProduct("   ", "invalid", 100m, 2, CancellationToken.None);

        Assert.IsFalse(result.Success);
        Assert.IsNull(result.Product);
        Assert.IsNotNull(result.Error);
        Assert.AreEqual("validation_error", result.Error.Code);
        var expected = new[] { "Name is required and must be 120 characters or fewer." };
        CollectionAssert.AreEqual(expected, result.Error.ValidationErrors?["Name"]);
    }

    [TestMethod]
    public async Task ListProducts_WhenServiceSucceeds_ReturnsProducts()
    {
        var tools = new ProductTools(new FakeProductService(
            products:
            [
                new ProductResponse(7, "測試商品", "描述", 99m, 4, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
            ]));

        var result = await tools.ListProducts(CancellationToken.None);

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Products);
        Assert.HasCount(1, result.Products);
        Assert.AreEqual(7, result.Products[0].Id);
    }

    [TestMethod]
    public void ProductRequestSchemaResource_IncludesValidationRules()
    {
        var json = ProductResources.GetProductRequestSchema();

        StringAssert.Contains(json, "\"maxLength\": 120");
        StringAssert.Contains(json, "\"notWhitespace\": true");
        StringAssert.Contains(json, "\"maximum\": 999999999");
    }

    [TestMethod]
    public void ProjectOverviewResource_IncludesAgentGuidance()
    {
        var json = ProductResources.GetProjectOverview();

        StringAssert.Contains(json, "\"name\": \"CrudApi\"");
        StringAssert.Contains(json, "health_check");
        StringAssert.Contains(json, "products_create");
        StringAssert.Contains(json, "backendMustBeRunning");
    }

    private sealed class FakeProductService(
        IReadOnlyList<ProductResponse>? products = null,
        ProductResponse? getProduct = null,
        ProductResponse? createdProduct = null,
        ProductResponse? updatedProduct = null,
        bool deleteResult = false) : IProductService
    {
        public Task<IReadOnlyList<ProductResponse>> GetProductsAsync() =>
            Task.FromResult(products ?? Array.Empty<ProductResponse>());

        public Task<ProductResponse?> GetProductAsync(int id) =>
            Task.FromResult(getProduct);

        public Task<ProductResponse> CreateProductAsync(CreateProductRequest request) =>
            Task.FromResult(createdProduct ?? new ProductResponse(
                1,
                request.Name,
                request.Description,
                request.Price,
                request.Stock,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow));

        public Task<ProductResponse?> UpdateProductAsync(int id, UpdateProductRequest request) =>
            Task.FromResult(updatedProduct);

        public Task<bool> DeleteProductAsync(int id) =>
            Task.FromResult(deleteResult);
    }
}
