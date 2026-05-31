using CrudApi.Dtos;
using CrudApi.Models;
using CrudApi.Services;
using CrudApi.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CrudApi.Tests;

[TestClass]
public sealed class ProductServiceTests
{
    [TestMethod]
    public async Task GetProductsAsync_ReturnsProductsOrderedByUpdatedAtDescending()
    {
        using var scope = new SqliteAppDbContextScope();
        scope.DbContext.Products.AddRange(
            new Product
            {
                Name = "舊商品",
                Description = null,
                Price = 100m,
                Stock = 1,
                CreatedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAt = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero)
            },
            new Product
            {
                Name = "新商品",
                Description = null,
                Price = 200m,
                Stock = 2,
                CreatedAt = new DateTimeOffset(2024, 1, 3, 0, 0, 0, TimeSpan.Zero),
                UpdatedAt = new DateTimeOffset(2024, 1, 4, 0, 0, 0, TimeSpan.Zero)
            });
        await scope.DbContext.SaveChangesAsync();

        var service = new ProductService(scope.DbContext);

        var products = await service.GetProductsAsync();

        Assert.HasCount(2, products);
        Assert.AreEqual("新商品", products[0].Name);
        Assert.AreEqual("舊商品", products[1].Name);
    }

    [TestMethod]
    public async Task CreateProductAsync_PersistsTrimmedValues()
    {
        using var scope = new SqliteAppDbContextScope();
        var service = new ProductService(scope.DbContext);

        var created = await service.CreateProductAsync(new CreateProductRequest(
            "  測試商品  ",
            "  測試描述  ",
            350m,
            5));

        var persisted = await scope.DbContext.Products.SingleAsync();

        Assert.IsGreaterThan(0, created.Id);
        Assert.AreEqual("測試商品", created.Name);
        Assert.AreEqual("測試描述", created.Description);
        Assert.AreEqual(created.Id, persisted.Id);
        Assert.AreEqual("測試商品", persisted.Name);
        Assert.AreEqual("測試描述", persisted.Description);
    }

    [TestMethod]
    public async Task UpdateProductAsync_WhenProductExists_ReturnsUpdatedProduct()
    {
        using var scope = new SqliteAppDbContextScope();
        var existing = new Product
        {
            Name = "原始商品",
            Description = "原始描述",
            Price = 150m,
            Stock = 2,
            CreatedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };
        scope.DbContext.Products.Add(existing);
        await scope.DbContext.SaveChangesAsync();

        var service = new ProductService(scope.DbContext);

        var updated = await service.UpdateProductAsync(existing.Id, new UpdateProductRequest(
            "  更新商品  ",
            "  更新描述  ",
            250m,
            9));

        var persisted = await scope.DbContext.Products.SingleAsync(product => product.Id == existing.Id);

        Assert.IsNotNull(updated);
        Assert.AreEqual(existing.Id, updated.Id);
        Assert.AreEqual("更新商品", updated.Name);
        Assert.AreEqual("更新描述", updated.Description);
        Assert.AreEqual(250m, updated.Price);
        Assert.AreEqual(9, updated.Stock);
        Assert.IsTrue(updated.UpdatedAt > existing.CreatedAt);
        Assert.AreEqual("更新商品", persisted.Name);
        Assert.AreEqual("更新描述", persisted.Description);
    }

    [TestMethod]
    public async Task UpdateProductAsync_WhenProductDoesNotExist_ReturnsNull()
    {
        using var scope = new SqliteAppDbContextScope();
        var service = new ProductService(scope.DbContext);

        var updated = await service.UpdateProductAsync(999, new UpdateProductRequest(
            "不存在",
            null,
            1m,
            1));

        Assert.IsNull(updated);
    }

    [TestMethod]
    public async Task DeleteProductAsync_WhenProductExists_RemovesProduct()
    {
        using var scope = new SqliteAppDbContextScope();
        var existing = new Product
        {
            Name = "待刪除商品",
            Description = null,
            Price = 80m,
            Stock = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        scope.DbContext.Products.Add(existing);
        await scope.DbContext.SaveChangesAsync();

        var service = new ProductService(scope.DbContext);

        var deleted = await service.DeleteProductAsync(existing.Id);

        Assert.IsTrue(deleted);
        Assert.AreEqual(0, await scope.DbContext.Products.CountAsync());
    }
}
