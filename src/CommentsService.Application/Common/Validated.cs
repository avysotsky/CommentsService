using System.Collections.ObjectModel;

namespace CommentsService.Application.Common;

public sealed class Validated<T> where T : class
{
    private Validated(T? value, IReadOnlyDictionary<string, string[]> errors)
    {
        Value = value;
        Errors = errors;
    }

    public T? Value { get; }
    public IReadOnlyDictionary<string, string[]> Errors { get; }
    public bool IsValid => Errors.Count == 0;

    public static Validated<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(value, new ReadOnlyDictionary<string, string[]>(new Dictionary<string, string[]>()));
    }

    public static Validated<T> Invalid(Dictionary<string, string[]> errors)
    {
        if (errors.Count == 0) throw new ArgumentException("Validation errors must not be empty.", nameof(errors));
        return new(null, new ReadOnlyDictionary<string, string[]>(errors.ToDictionary()));
    }
}
