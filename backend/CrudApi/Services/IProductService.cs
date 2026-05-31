using CrudApi.Dtos;

namespace CrudApi.Services;

public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> GetProductsAsync();

    Task<ProductResponse?> GetProductAsync(int id);

    Task<ProductResponse> CreateProductAsync(CreateProductRequest request);

    Task<ProductResponse?> UpdateProductAsync(int id, UpdateProductRequest request);

    Task<bool> DeleteProductAsync(int id);
}
