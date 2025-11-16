using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace SSC.Net.JSONRPCClient;

/// <summary>
/// JsonRPCResult
/// </summary>
public sealed class JsonRpcClientResult
{
    internal const string SerializationUnreferencedCodeMessage
        = "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.";
    internal const string SerializationDynamicCodeMessage
        = "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.";

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public JsonElement Result;
    public JsonElement Error;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get JObject from serialized JSON
    /// </summary>
    /// <param name="p_Deserialized">Input</param>
    /// <param name="p_JObject">Result object</param>
    /// <returns></returns>
    [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SerializationDynamicCodeMessage)]
    public bool TryGet<T>(out T p_JObject)
        where T : class, new()
    {
        p_JObject = null;
        try
        {
            p_JObject = Result.Deserialize<T>();
        }
        catch (Exception)
        {
            return false;
        }

        return p_JObject != null;
    }
}
