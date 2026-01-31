namespace SSC.Extensions;

/// <summary>
/// String extension methods
/// </summary>
public static class StringExtensions
{
    public static string ToSnakeCase(this string instance)
        => System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(instance);

    public static string ToCamelCase(this string instance)
        => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(instance);
}
