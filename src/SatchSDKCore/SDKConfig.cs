using System.Text.Json;

namespace SSC;

public static class SDKConfig
{
    internal const string SerializationUnreferencedCodeMessage
        = "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.";

    internal const string SerializationDynamicCodeMessage
        = "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.";


    public static JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = false };
}
