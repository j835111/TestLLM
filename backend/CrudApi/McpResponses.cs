using CrudApi.Dtos;

namespace CrudApi.Mcp.Contracts;

public sealed record ErrorDetails(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);

public sealed record HealthCheckResult(
    bool Success,
    string? Status = null,
    string? Service = null,
    ErrorDetails? Error = null);

public sealed record ProductListResult(
    bool Success,
    IReadOnlyList<ProductResponse>? Products = null,
    ErrorDetails? Error = null);

public sealed record ProductItemResult(
    bool Success,
    ProductResponse? Product = null,
    ErrorDetails? Error = null);

public sealed record DeleteProductResult(
    bool Success,
    bool Deleted,
    int Id,
    ErrorDetails? Error = null);
