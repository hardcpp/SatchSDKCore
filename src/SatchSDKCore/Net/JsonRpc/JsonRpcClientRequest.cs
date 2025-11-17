using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SSC.Net.JsonRpc;

/// <summary>
/// JsonRpc request
/// </summary>
public class JsonRpcClientRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpcVersion { get; init; } = "2.0";

    [JsonPropertyName("method")]
    public required string Method { get; init; }

    [JsonPropertyName("params")]
    public JsonElement? Params { get; init; }

    [JsonPropertyName("id")]
    public int Id { get; init; } = 1;
}