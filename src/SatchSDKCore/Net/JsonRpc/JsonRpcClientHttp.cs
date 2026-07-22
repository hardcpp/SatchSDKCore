using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using SSC.Net.HttpEx;
using static SSC.Net.JsonRpc.IJsonRpcClient;

namespace SSC.Net.JsonRpc;

/// <summary>
/// HTTP implementation of the IJsonRpcClient
/// </summary>
public class JsonRpcClientHttp : JsonRpcClientBase
{
    public IHttpClientEx HttpClient { get; }

    public string? OverrideUrl { get; }


    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="httpClient">Http client to use</param>
    /// <param name="overrideUrl">Url override?</param>
    public JsonRpcClientHttp(IHttpClientEx httpClient, string? overrideUrl = null)
    {
        HttpClient  = httpClient;
        OverrideUrl = overrideUrl;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc />
    [RequiresUnreferencedCode(SDKConfig.SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SDKConfig.SerializationDynamicCodeMessage)]
    protected override JsonRpcClientResult? DoCall(
        JsonRpcClientRequest request,
        ECallOptions         options
    )
    {
        HttpClientExResponse httpResult = HttpClient.DoRequest(
            "POST",
            OverrideUrl ?? string.Empty,
            HttpClientExPayload.FromJsonString(JsonSerializer
                                                   .Serialize(request,
                                                              SDKConfig.JsonSerializerOptions)),
            CallOptionsToRequestOptions(options)
        );

        return BuildJSONRPCClientResult(request, httpResult);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode(SDKConfig.SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SDKConfig.SerializationDynamicCodeMessage)]
    protected override void DoCallInBackground(
        JsonRpcClientRequest          request,
        CancellationToken             cancellationToken,
        Action<JsonRpcClientResult?>? callback,
        ECallOptions                  options
    )
    {
        HttpClient.DoRequestInBackground(
            "POST",
            OverrideUrl ?? string.Empty,
            cancellationToken,
            httpResult => { callback?.Invoke(BuildJSONRPCClientResult(request, httpResult)); },
            HttpClientExPayload.FromJsonString(JsonSerializer.Serialize(request,
                                                                        SDKConfig.JsonSerializerOptions)),
            CallOptionsToRequestOptions(options)
        );
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode(SDKConfig.SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SDKConfig.SerializationDynamicCodeMessage)]
    protected override async Task<JsonRpcClientResult?> DoCallAsync(
        JsonRpcClientRequest request,
        CancellationToken    cancellationToken,
        ECallOptions         options
    )
    {
        HttpClientExResponse httpResult = await HttpClient.DoRequestAsync(
            "POST",
            OverrideUrl ?? string.Empty,
            cancellationToken,
            HttpClientExPayload
                .FromJsonString(JsonSerializer
                                    .Serialize(request,
                                               SDKConfig
                                                   .JsonSerializerOptions)),
            CallOptionsToRequestOptions(options)
        ).ConfigureAwait(false);

        return BuildJSONRPCClientResult(request, httpResult);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Handle a http client ex response
    /// </summary>
    /// <param name="request">Request informations</param>
    /// <param name="httpResponse">Http client ex response</param>
    /// <returns></returns>
    private static JsonRpcClientResult? BuildJSONRPCClientResult(
        JsonRpcClientRequest  request,
        HttpClientExResponse? httpResponse)
    {
        if (httpResponse == null || string.IsNullOrEmpty(httpResponse.BodyString))
            return null;

        try
        {
            var jsonResult = JsonObject.Parse(httpResponse.BodyString) as JsonObject;

            return new JsonRpcClientResult
            {
                Result = (jsonResult?["result"] ?? null) as JsonObject,
                Error  = (jsonResult?["error"]  ?? null) as JsonObject
            };
        }
        catch (Exception exception)
        {
            Logging.Log(ELogSeverity.Error,
                        $"[Net.JsonRPC][JsonRPCClient.JsonRpcClientHttp] Request {request.Method} failed parsing response:");
            Logging.Log(ELogSeverity.Error, exception);
        }

        return null;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Transform call options flags to request options flags
    /// </summary>
    /// <param name="callOptions"></param>
    /// <returns></returns>
    private IHttpClientEx.ERequestOptions CallOptionsToRequestOptions(ECallOptions callOptions)
    {
        var requestOptions = IHttpClientEx.ERequestOptions.None;
        if (callOptions.HasFlag(ECallOptions.IgnoreRetryPolicy))
            requestOptions |= IHttpClientEx.ERequestOptions.IgnoreRetryPolicy;

        return requestOptions;
    }
}
