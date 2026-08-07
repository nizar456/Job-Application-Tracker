namespace Backend.Services;

public sealed record CommandResult(
    bool IsSuccess,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    bool NotFound = false)
{
    public static CommandResult Ok() => new(true);

    public static CommandResult Failed(IReadOnlyDictionary<string, string[]> errors) => new(false, Errors: errors);

    public static CommandResult Missing() => new(false, NotFound: true);
}

public sealed record CommandResult<T>(
    bool IsSuccess,
    T? Value = default,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    bool NotFound = false)
{
    public static CommandResult<T> Ok(T value) => new(true, Value: value);

    public static CommandResult<T> Failed(IReadOnlyDictionary<string, string[]> errors) => new(false, Errors: errors);

    public static CommandResult<T> Missing() => new(false, NotFound: true);
}
