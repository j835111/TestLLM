using CrudApi.Data;
using CrudApi.Dtos;
using CrudApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CrudApi.Services;

public sealed class ProductService(AppDbContext dbContext) : IProductService
{
    public async Task<IReadOnlyList<ProductResponse>> GetProductsAsync()
    {
        var products = await dbContext.Products
            .AsNoTracking()
            .Select(product => new ProductResponse(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.Stock,
                product.CreatedAt,
                product.UpdatedAt))
            .ToListAsync();

        return products
            .OrderByDescending(product => product.UpdatedAt)
            .ToList();
    }

    public async Task<ProductResponse?> GetProductAsync(int id)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        return product?.ToResponse();
    }

    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Price = request.Price,
            Stock = request.Stock,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        return product.ToResponse();
    }

    public async Task<ProductResponse?> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        var product = await dbContext.Products.FindAsync(id);
        if (product is null)
        {
            return null;
        }

        product.Name = request.Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync();
        return product.ToResponse();
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var deleted = await dbContext.Products
            .Where(product => product.Id == id)
            .ExecuteDeleteAsync();

        return deleted > 0;
    }
}
