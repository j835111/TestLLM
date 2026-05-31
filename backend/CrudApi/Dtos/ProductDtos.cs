using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using CrudApi.Models;
using CrudApi.Validation;

namespace CrudApi.Dtos;

[ExcludeFromCodeCoverage]
public sealed record ProductResponse(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

[ExcludeFromCodeCoverage]
public sealed record CreateProductRequest(
    [NotEmptyOrWhitespace(ErrorMessage = "Name is required and must be 120 characters or fewer.")]
    [StringLength(120, ErrorMessage = "Name is required and must be 120 characters or fewer.")]
    string Name,
    [StringLength(1000)] string? Description,
    [Range(0, 999_999_999)] decimal Price,
    [Range(0, int.MaxValue)] int Stock);

[ExcludeFromCodeCoverage]
public sealed record UpdateProductRequest(
    [NotEmptyOrWhitespace(ErrorMessage = "Name is required and must be 120 characters or fewer.")]
    [StringLength(120, ErrorMessage = "Name is required and must be 120 characters or fewer.")]
    string Name,
    [StringLength(1000)] string? Description,
    [Range(0, 999_999_999)] decimal Price,
    [Range(0, int.MaxValue)] int Stock);

public static class ProductMappings
{
    public static ProductResponse ToResponse(this Product product) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.Stock,
        product.CreatedAt,
        product.UpdatedAt);
}
