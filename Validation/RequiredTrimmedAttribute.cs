using System.ComponentModel.DataAnnotations;

namespace LionttoMoveis.Validation
{
    /// <summary>
    /// Similar to Required, but also rejects values containing only whitespace.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class RequiredTrimmedAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
                return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} is required.");

            if (value is string text && string.IsNullOrWhiteSpace(text))
                return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} is required.");

            return ValidationResult.Success;
        }
    }
}
