using System;
using System.Text.Json;

namespace SSC.Net.JSONRPCClient;

/// <summary>
/// JsonRPCResult
/// </summary>
public sealed class JsonRpcClientResult
{
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
    public bool TryGet<T>(out T p_JObject)
        where T : class, new()
    {
        p_JObject = null;
        try
        {
            //p_JObject = Result.ToObject<T>();
        }
        catch (Exception)
        {
            return false;
        }

        return p_JObject != null;
    }
}
