using SSC.Net.HTTPClient;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace SSC.Net.JSONRPCClient;

/// <summary>
/// HTTP implementation of the IJsonRpcClient
/// </summary>
public class JsonRpcClientHttp : IJsonRpcClient
{
    private readonly IHTTPClient _httpClient;
    private readonly string?     _overrideURL;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public IHTTPClient HTTPClient  => _httpClient;
    public string?     OverrideURL => _overrideURL;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="httpClient">HTTP client to use</param>
    public JsonRpcClientHttp(IHTTPClient httpClient, string? overrideURL = null)
    {
        _httpClient  = httpClient;
        _overrideURL = overrideURL;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc/>
    [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SerializationDynamicCodeMessage)]
    protected override JsonRpcClientResult? DoCall(
        JsonRpcClientRequest request,
        ECallOptions options
    )
    {
        var httpResult = _httpClient.DoRequest(
            "POST",
            _overrideURL ?? string.Empty,
            HTTPClientPayload.FromJson(request, indend: false),
            CallOptionsToRequestOptions(options)
        );

        return BuildJSONRPCClientResult(request, httpResult);
    }
    /// <inheritdoc/>
    [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SerializationDynamicCodeMessage)]
    protected override void DoCallInBackground(
        JsonRpcClientRequest          request,
        CancellationToken             cancellationToken,
        Action<JsonRpcClientResult?>? callback,
        ECallOptions                  options
    )
    {
        _httpClient.DoRequestInBackground(
            "POST",
            _overrideURL ?? string.Empty,
            cancellationToken,
            (httpResult) => { callback?.Invoke(BuildJSONRPCClientResult(request, httpResult)); },
            HTTPClientPayload.FromJson(request, indend: false),
            CallOptionsToRequestOptions(options)
        );
    }
    /// <inheritdoc/>
    [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SerializationDynamicCodeMessage)]
    protected override async Task<JsonRpcClientResult?> DoCallAsync(
        JsonRpcClientRequest request,
        CancellationToken    cancellationToken,
        ECallOptions         options
    )
    {
        var httpResult = await _httpClient.DoRequestAsync(
            "POST",
            _overrideURL ?? string.Empty,
            cancellationToken,
            HTTPClientPayload.FromJson(request, indend: false),
            CallOptionsToRequestOptions(options)
        ).ConfigureAwait(false);

        return BuildJSONRPCClientResult(request, httpResult);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Handle a web response
    /// </summary>
    /// <param name="p_Method">Called method</param>
    /// <param name="p_WebResponse">Web response</param>
    /// <returns></returns>
    private static JsonRpcClientResult BuildJSONRPCClientResult(JsonRpcClientRequest request, HTTPClientResponse? httpResponse)
    {
        /*if (p_WebResponse == null)
            return new JsonRPCResult() { RawResponse = p_WebResponse, Result = null };

        try
        {
            var l_JsonResult = JObject.Parse(p_WebResponse.BodyString);

            return new JsonRPCResult()
            {
                RawResponse = p_WebResponse,
                Result = (l_JsonResult.GetValue("result") ?? null) as JObject,
                Error = (l_JsonResult.GetValue("error") ?? null) as JObject
            };
        }
        catch (Exception l_Exception)
        {
            Logger.Log(ELogLevel.Error, $"[CP_API_SDK.Network][JsonRPCClient.HandleResponse] Request {p_Method} failed parsing response:");
            Logger.Log(ELogLevel.Error, l_Exception);
        }*/

        return null;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Transform call options flags to request options flags
    /// </summary>
    /// <param name="callOptions"></param>
    /// <returns></returns>
    private IHTTPClient.ERequestOptions CallOptionsToRequestOptions(ECallOptions callOptions)
    {
        var requestOptions = IHTTPClient.ERequestOptions.None;
        if (callOptions.HasFlag(ECallOptions.IgnoreRetryPolicy))
            requestOptions |= IHTTPClient.ERequestOptions.IgnoreRetryPolicy;

        return requestOptions;
    }
}
