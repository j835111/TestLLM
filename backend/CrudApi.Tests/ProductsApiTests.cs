using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using CrudApi.Dtos;
using CrudApi.Tests.Infrastructure;

namespace CrudApi.Tests;

[TestClass]
public sealed class ProductsApiTests
{
    private CrudApiApplicationFactory factory = null!;
    private HttpClient client = null!;

    [TestInitialize]
    public void Initialize()
    {
        factory = new CrudApiApplicationFactory(Guid.NewGuid().ToString("N"));
        client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [TestCleanup]
    public void Cleanup()
    {
        client.Dispose();
        factory.Dispose();
    }

    [TestMethod]
    public async Task GetProducts_WhenDatabaseIsEmpty_ReturnsEmptyList()
    {
        var products = await client.GetFromJsonAsync<List<ProductResponse>>("/api/products");

        Assert.IsNotNull(products);
        Assert.IsEmpty(products);
    }

    [TestMethod]
    public async Task CreateProduct_WithValidPayload_ReturnsCreatedProduct()
    {
        var request = new CreateProductRequest(
            "LLM 評測套件",
            "用來驗證 CRUD 流程",
            1200m,
            8);

        var response = await client.PostAsJsonAsync("/api/products", request);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        Assert.IsNotNull(product);
        Assert.AreEqual(request.Name, product.Name);
        Assert.AreEqual(request.Description, product.Description);
        Assert.AreEqual(request.Price, product.Price);
        Assert.AreEqual(request.Stock, product.Stock);
        Assert.IsGreaterThan(0, product.Id);
        Assert.IsTrue(response.Headers.Location?.AbsolutePath.EndsWith($"/api/Products/{product.Id}", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    [TestMethod]
    public async Task CreateProduct_WithWhitespaceName_ReturnsValidationProblem()
    {
        var request = new CreateProductRequest(
            "   ",
            "invalid",
            100m,
            2);

        var response = await client.PostAsJsonAsync("/api/products", request);
        var validationProblem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.IsNotNull(validationProblem);
        CollectionAssert.Contains(validationProblem.Errors["Name"], "Name is required and must be 120 characters or fewer.");
    }

    [TestMethod]
    public async Task UpdateProduct_WhenProductExists_ReturnsUpdatedProduct()
    {
        var created = await CreateProductAsync(new CreateProductRequest(
            "原始商品",
            "原始描述",
            500m,
            3));

        var request = new UpdateProductRequest(
            "更新後商品",
            "更新後描述",
            750m,
            10);

        var response = await client.PutAsJsonAsync($"/api/products/{created.Id}", request);
        var updated = await response.Content.ReadFromJsonAsync<ProductResponse>();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(updated);
        Assert.AreEqual(created.Id, updated.Id);
        Assert.AreEqual(request.Name, updated.Name);
        Assert.AreEqual(request.Description, updated.Description);
        Assert.AreEqual(request.Price, updated.Price);
        Assert.AreEqual(request.Stock, updated.Stock);
        Assert.IsTrue(updated.UpdatedAt >= created.UpdatedAt);
    }

    [TestMethod]
    public async Task DeleteProduct_WhenProductExists_RemovesProduct()
    {
        var created = await CreateProductAsync(new CreateProductRequest(
            "待刪除商品",
            null,
            300m,
            1));

        var deleteResponse = await client.DeleteAsync($"/api/products/{created.Id}");
        var getResponse = await client.GetAsync($"/api/products/{created.Id}");

        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<ProductResponse> CreateProductAsync(CreateProductRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/products", request);
        response.EnsureSuccessStatusCode();

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.IsNotNull(product);
        return product;
    }
}
