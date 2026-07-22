using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SSC.Net.JsonRpc;

/// <summary>
/// JsonRPCResult
/// </summary>
public sealed class JsonRpcClientResult
{
    public JsonObject? Error;
    public JsonObject? Result;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get JObject from serialized JSON
    /// </summary>
    /// <param name="p_Deserialized">Input</param>
    /// <param name="result">Result object</param>
    /// <returns></returns>
    [RequiresUnreferencedCode(SDKConfig.SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SDKConfig.SerializationDynamicCodeMessage)]
    public bool TryGet<T>(out T? result)
        where T : class, new()
    {
        result = null;
        try
        {
            result = Result.Deserialize<T>(SDKConfig.JsonSerializerOptions);
        }
        catch (Exception)
        {
            return false;
        }

        return result != null;
    }
}
