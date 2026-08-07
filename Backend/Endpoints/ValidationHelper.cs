using System.ComponentModel.DataAnnotations;

namespace Backend.Endpoints;

public static class ValidationHelper
{
    public static IResult? Validate<T>(T request) where T : class
    {
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true))
        {
            return null;
        }

        return Results.ValidationProblem(results
            .GroupBy(r => r.MemberNames.FirstOrDefault() ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.Select(r => r.ErrorMessage!).ToArray()));
    }
}
