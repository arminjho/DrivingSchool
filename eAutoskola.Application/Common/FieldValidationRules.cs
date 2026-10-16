using System.Text.RegularExpressions;
using eAutoskola.Domain.Resources;
using eAutoskola.Domain.Exceptions;

namespace eAutoskola.Application.Common;

/// <summary>
/// Hand-written, shared validation helpers (no FluentValidation dependency - see
/// docs/REFERENCE_ANALYSIS.md §2.4). Every message explicitly states the expected format, per
/// upute.md §4. Callers collect errors into a dictionary and throw ONE ValidationException at
/// the end, so a single request reports every invalid field at once.
/// </summary>
public static partial class FieldValidationRules
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^\+387\s?6\d(\s?\d{3}){2}$")]
    private static partial Regex PhonePattern();

    public static void RequireText(IDictionary<string, string[]> errors, string field, string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Add(errors, field, Messages.RequiredField);
        }
        else if (value.Length > maxLength)
        {
            Add(errors, field, $"{label} ne smije imati više od {maxLength} znakova.");
        }
    }

    public static void RequireEmail(IDictionary<string, string[]> errors, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !EmailPattern().IsMatch(value))
        {
            Add(errors, field, Messages.InvalidEmailFormat);
        }
    }

    public static void RequirePhone(IDictionary<string, string[]> errors, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !PhonePattern().IsMatch(value))
        {
            Add(errors, field, Messages.InvalidPhoneFormat);
        }
    }

    public static void OptionalPhone(IDictionary<string, string[]> errors, string field, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !PhonePattern().IsMatch(value))
        {
            Add(errors, field, Messages.InvalidPhoneFormat);
        }
    }

    public static void RequirePassword(IDictionary<string, string[]> errors, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 8)
        {
            Add(errors, field, Messages.PasswordTooShort);
        }
    }

    public static void Add(IDictionary<string, string[]> errors, string field, string message)
    {
        errors[field] = errors.TryGetValue(field, out var existing) ? [.. existing, message] : [message];
    }

    /// <summary>Throws if any errors were collected - call once at the end of a validation pass.</summary>
    public static void ThrowIfAny(IDictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
