using System.ComponentModel.DataAnnotations;

namespace CrudApi.Validation;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotEmptyOrWhitespaceAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is string text && !string.IsNullOrWhiteSpace(text);
}
