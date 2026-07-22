using System.Text.Json;

namespace SSC.Extensions;

/// <summary>
/// String extension methods
/// </summary>
public static class StringExtensions
{
    public static string ToSnakeCase(this string instance)
        => JsonNamingPolicy.SnakeCaseLower.ConvertName(instance);

    public static string ToCamelCase(this string instance)
        => JsonNamingPolicy.CamelCase.ConvertName(instance);
}
