using System.Text.RegularExpressions;

namespace AIDebugPro.Services.Utilities;

/// <summary>
/// Validation helper methods
/// </summary>
public static class Validator
{
    /// <summary>
    /// Validates that a value is not null
    /// </summary>
    public static T NotNull<T>(T value, string parameterName) where T : class
    {
        if (value == null)
            throw new ArgumentNullException(parameterName);
        return value;
    }

    /// <summary>
    /// Validates that a string is not null or empty
    /// </summary>
    public static string NotNullOrEmpty(string value, string parameterName)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Value cannot be null or empty", parameterName);
        return value;
    }

    /// <summary>
    /// Validates that a string is not null or whitespace
    /// </summary>
    public static string NotNullOrWhiteSpace(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace", parameterName);
        return value;
    }

    /// <summary>
    /// Validates that a number is in range
    /// </summary>
    public static T InRange<T>(T value, T min, T max, string parameterName) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            throw new ArgumentOutOfRangeException(parameterName, $"Value must be between {min} and {max}");
        return value;
    }

    /// <summary>
    /// Validates that a collection is not null or empty
    /// </summary>
    public static IEnumerable<T> NotNullOrEmpty<T>(IEnumerable<T> collection, string parameterName)
    {
        if (collection == null || !collection.Any())
            throw new ArgumentException("Collection cannot be null or empty", parameterName);
        return collection;
    }

    /// <summary>
    /// Validates an email address
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates a URL
    /// </summary>
    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Validates a GUID
    /// </summary>
    public static bool IsValidGuid(string guid)
    {
        return Guid.TryParse(guid, out _);
    }

    /// <summary>
    /// Validates that a file exists
    /// </summary>
    public static string FileExists(string path, string parameterName)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}", parameterName);
        return path;
    }

    /// <summary>
    /// Validates that a directory exists
    /// </summary>
    public static string DirectoryExists(string path, string parameterName)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        return path;
    }
}

/// <summary>
/// Result of a validation operation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ValidationResult Success() => new() { IsValid = true };
    
    public static ValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };

    public void AddError(string error)
    {
        IsValid = false;
        Errors.Add(error);
    }
}

/// <summary>
/// Fluent validation builder
/// </summary>
public class ValidationBuilder<T>
{
    private readonly T _value;
    private readonly List<string> _errors = new();

    public ValidationBuilder(T value)
    {
        _value = value;
    }

    public ValidationBuilder<T> Must(Func<T, bool> predicate, string errorMessage)
    {
        if (!predicate(_value))
        {
            _errors.Add(errorMessage);
        }
        return this;
    }

    public ValidationBuilder<T> NotNull(string errorMessage = "Value cannot be null")
    {
        if (_value == null)
        {
            _errors.Add(errorMessage);
        }
        return this;
    }

    public ValidationBuilder<T> When(Func<T, bool> condition, Action<ValidationBuilder<T>> validation)
    {
        if (condition(_value))
        {
            validation(this);
        }
        return this;
    }

    public ValidationResult Build()
    {
        return _errors.Any()
            ? ValidationResult.Failure(_errors.ToArray())
            : ValidationResult.Success();
    }
}

public static class ValidationBuilderExtensions
{
    public static ValidationBuilder<string> NotEmpty(this ValidationBuilder<string> builder, string errorMessage = "Value cannot be empty")
    {
        return builder.Must(s => !string.IsNullOrEmpty(s), errorMessage);
    }

    public static ValidationBuilder<string> NotWhiteSpace(this ValidationBuilder<string> builder, string errorMessage = "Value cannot be whitespace")
    {
        return builder.Must(s => !string.IsNullOrWhiteSpace(s), errorMessage);
    }

    public static ValidationBuilder<string> MinLength(this ValidationBuilder<string> builder, int minLength, string? errorMessage = null)
    {
        errorMessage ??= $"Value must be at least {minLength} characters";
        return builder.Must(s => s?.Length >= minLength, errorMessage);
    }

    public static ValidationBuilder<string> MaxLength(this ValidationBuilder<string> builder, int maxLength, string? errorMessage = null)
    {
        errorMessage ??= $"Value must not exceed {maxLength} characters";
        return builder.Must(s => s?.Length <= maxLength, errorMessage);
    }

    public static ValidationBuilder<string> Email(this ValidationBuilder<string> builder, string errorMessage = "Invalid email address")
    {
        return builder.Must(s => Validator.IsValidEmail(s), errorMessage);
    }

    public static ValidationBuilder<string> Url(this ValidationBuilder<string> builder, string errorMessage = "Invalid URL")
    {
        return builder.Must(s => Validator.IsValidUrl(s), errorMessage);
    }

    public static ValidationBuilder<T> InRange<T>(this ValidationBuilder<T> builder, T min, T max, string? errorMessage = null) where T : IComparable<T>
    {
        errorMessage ??= $"Value must be between {min} and {max}";
        return builder.Must(v => v.CompareTo(min) >= 0 && v.CompareTo(max) <= 0, errorMessage);
    }
}
