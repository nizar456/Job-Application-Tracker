namespace Backend.Services;

public static class FieldErrors
{
    public static IReadOnlyDictionary<string, string[]> For(string field, string message) =>
        new Dictionary<string, string[]> { [field] = [message] };
}
