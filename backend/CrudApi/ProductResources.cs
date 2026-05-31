using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace CrudApi.Mcp.Resources;

[McpServerResourceType]
public sealed class ProductResources
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    [McpServerResource(
        UriTemplate = "crudapi://project/overview",
        Name = "project_overview",
        Title = "Project Overview",
        MimeType = "application/json")]
    [Description("High-level project guide for agents using CrudApi through MCP.")]
    public static string GetProjectOverview() => Serialize(new
    {
        project = new
        {
            name = "CrudApi",
            purpose = "A product CRUD backend exposed through MCP for natural-language operations.",
            primaryEntity = "Product"
        },
        recommendedWorkflow = new[]
        {
            "Use health_check first to confirm the backend is reachable.",
            "Use products_list to inspect current data before mutating it.",
            "Use products_get when the user references a specific product id.",
            "Use products_create, products_update, and products_delete for mutations."
        },
        tools = new[]
        {
            new { name = "health_check", category = "read", purpose = "Check backend health." },
            new { name = "products_list", category = "read", purpose = "List all products." },
            new { name = "products_get", category = "read", purpose = "Fetch one product by id." },
            new { name = "products_create", category = "write", purpose = "Create a product." },
            new { name = "products_update", category = "write", purpose = "Update a product." },
            new { name = "products_delete", category = "write", purpose = "Delete a product." }
        },
        validationRules = new
        {
            name = "required, non-whitespace, max 120 characters",
            description = "optional, max 1000 characters",
            price = "decimal, minimum 0, maximum 999999999",
            stock = "integer, minimum 0"
        },
        dependencies = new
        {
            mcpEndpoint = "/mcp",
            backendMustBeRunning = true
        },
        outOfScope = new[]
        {
            "No full-text search.",
            "No pagination.",
            "No batch operations.",
            "No authentication in the current MCP prototype."
        }
    });

    [McpServerResource(
        UriTemplate = "crudapi://products/schema/request",
        Name = "products_request_schema",
        Title = "Products Request Schema",
        MimeType = "application/json")]
    [Description("Validation rules for create and update product requests.")]
    public static string GetProductRequestSchema() => Serialize(new
    {
        name = new
        {
            type = "string",
            required = true,
            notWhitespace = true,
            maxLength = 120
        },
        description = new
        {
            type = "string|null",
            required = false,
            maxLength = 1000
        },
        price = new
        {
            type = "decimal",
            minimum = 0,
            maximum = 999_999_999
        },
        stock = new
        {
            type = "integer",
            minimum = 0
        }
    });

    [McpServerResource(
        UriTemplate = "crudapi://products/schema/response",
        Name = "products_response_schema",
        Title = "Products Response Schema",
        MimeType = "application/json")]
    [Description("Shape of the ProductResponse returned by CrudApi.")]
    public static string GetProductResponseSchema() => Serialize(new
    {
        id = "integer",
        name = "string",
        description = "string|null",
        price = "decimal",
        stock = "integer",
        createdAt = "date-time",
        updatedAt = "date-time"
    });

    [McpServerResource(
        UriTemplate = "crudapi://products/examples/create",
        Name = "products_create_examples",
        Title = "Create Product Examples",
        MimeType = "application/json")]
    [Description("Examples for successful and invalid product creation requests.")]
    public static string GetCreateExamples() => Serialize(new
    {
        validRequest = new
        {
            name = "LLM 評測套件",
            description = "用來驗證 CRUD 流程",
            price = 1200,
            stock = 8
        },
        validationError = new
        {
            error = "validation_error",
            message = "The request payload is invalid.",
            details = new
            {
                name = new[]
                {
                    "Name is required and must be 120 characters or fewer."
                }
            }
        }
    });

    [McpServerResource(
        UriTemplate = "crudapi://products/examples/update",
        Name = "products_update_examples",
        Title = "Update Product Examples",
        MimeType = "application/json")]
    [Description("Examples for successful and missing product update operations.")]
    public static string GetUpdateExamples() => Serialize(new
    {
        validRequest = new
        {
            id = 42,
            name = "更新後商品",
            description = "更新後描述",
            price = 750,
            stock = 10
        },
        notFound = new
        {
            error = "not_found",
            message = "Product 42 was not found."
        }
    });

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, SerializerOptions);
}
