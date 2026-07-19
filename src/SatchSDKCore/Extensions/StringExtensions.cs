namespace SSC.Extensions;

/// <summary>
/// String extension methods
/// </summary>
public static class StringExtensions
{
    extension(string instance)
    {
        public string ToSnakeCase()
            => System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(instance);

        public string ToCamelCase()
            => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(instance);
    }
}
