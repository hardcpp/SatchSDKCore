using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using static SSC.Net.JsonRpc.IJsonRpcClient;

namespace SSC.Net.JsonRpc;

/// <summary>
/// Abstract base implementation of the IJsonRpcClient
/// </summary>
public abstract class JsonRpcClientBase : IJsonRpcClient
{
    /// <summary>
    /// Do a sync call
    /// </summary>
    /// <param name="method">Method to call</param>
    /// <param name="parameters">Method parameters</param>
    /// <param name="options">Call options</param>
    /// <returns>The response if the call reached the server</returns>
    [RequiresUnreferencedCode(SDKConfig.SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SDKConfig.SerializationDynamicCodeMessage)]
    public JsonRpcClientResult? Call(
        string method,
        IEnumerable<object> parameters,
        ECallOptions options = ECallOptions.None
    )
    {
        var request = new JsonRpcClientRequest
        {
            Method = method,
            Params = JsonSerializer.SerializeToElement(parameters, SDKConfig.JsonSerializerOptions)
        };

        return DoCall(request, options);
    }
    /// <summary>
    /// Do a sync call
    /// </summary>
    /// <param name="method">Method to call</param>
    /// <param name="parameters">Method parameters</param>
    /// <param name="options">Call options</param>
    /// <returns>The response if the call reached the server</returns>
    [RequiresUnreferencedCode(SDKConfig.SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SDKConfig.SerializationDynamicCodeMessage)]
    public JsonRpcClientResult? Call(
        string method,
        IReadOnlyDictionary<string, object> parameters,
        ECallOptions options = ECallOptions.None
    )
    {
        var request = new JsonRpcClientRequest
        {
            Method = method,
            Params = JsonSerializer.SerializeToElement(parameters, SDKConfig.JsonSerializerOptions)
        };

        return DoCall(request, options);
    }
    /// <summary>
    /// Do a non-blocking call in the background with a callback
    /// </summary>
    /// <param name="method">Method to call</param>
    /// <param name="parameters">Method parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="callback">Callback</param>
    /// <param name="options">Call options</param>
    [RequiresUnreferencedCode(SDKConfig.SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SDKConfig.SerializationDynamicCodeMessage)]
    public void CallInBackground(
        string method,
        IEnumerable<object> parameters,
        CancellationToken cancellationToken,
        Action<JsonRpcClientResult?>? callback,
        ECallOptions options = ECallOptions.None
    )
    {
        var request = new JsonRpcClientRequest
        {
            Method = method,
            Params = JsonSerializer.SerializeToElement(parameters, SDKConfig.JsonSerializerOptions)
        };

        DoCallInBackground(request, cancellationToken, callback, options);
    }
    /// <summary>
    /// Do a non-blocking call in the background with a callback
    /// </summary>
    /// <param name="method">Method to call</param>
    /// <param name="parameters">Method parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="callback">Callback</param>
    /// <param name="options">Call options</param>
    [RequiresUnreferencedCode(SDKConfig.SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SDKConfig.SerializationDynamicCodeMessage)]
    public void CallInBackground(
        string method,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken cancellationToken,
        Action<JsonRpcClientResult?>? callback,
        ECallOptions options = ECallOptions.None
    )
    {
        var request = new JsonRpcClientRequest
        {
            Method = method,
            Params = JsonSerializer.SerializeToElement(parameters, SDKConfig.JsonSerializerOptions)
        };

        DoCallInBackground(request, cancellationToken, callback, options);
    }
    /// <summary>
    /// Do an async call
    /// </summary>
    /// <param name="method">Method to call</param>
    /// <param name="parameters">Method parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="options">Call options</param>
    /// <returns>The response if the call reached the server</returns>
    [RequiresUnreferencedCode(SDKConfig.SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SDKConfig.SerializationDynamicCodeMessage)]
    public Task<JsonRpcClientResult?> CallAsync(
        string method,
        IEnumerable<object> parameters,
        CancellationToken cancellationToken,
        ECallOptions options = ECallOptions.None
    )
    {
        var request = new JsonRpcClientRequest
        {
            Method = method,
            Params = JsonSerializer.SerializeToElement(parameters, SDKConfig.JsonSerializerOptions)
        };

        return DoCallAsync(request, cancellationToken, options);
    }

    /// <summary>
    /// Do an async call
    /// </summary>
    /// <param name="method">Method to call</param>
    /// <param name="parameters">Method parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="options">Call options</param>
    /// <returns>The response if the call reached the server</returns>
    [RequiresUnreferencedCode(SDKConfig.SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SDKConfig.SerializationDynamicCodeMessage)]
    public Task<JsonRpcClientResult?> CallAsync(
        string method,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken cancellationToken,
        ECallOptions options = ECallOptions.None
    )
    {
        var request = new JsonRpcClientRequest
        {
            Method = method,
            Params = JsonSerializer.SerializeToElement(parameters, SDKConfig.JsonSerializerOptions)
        };

        return DoCallAsync(request, cancellationToken, options);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Do a sync call
    /// </summary>
    /// <param name="request">Call request</param>
    /// <param name="options">Call options</param>
    /// <returns>The response if the call reached the server</returns>
    [RequiresUnreferencedCode(SDKConfig.SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SDKConfig.SerializationDynamicCodeMessage)]
    protected abstract JsonRpcClientResult? DoCall(
        JsonRpcClientRequest request,
        ECallOptions options
    );
    /// <summary>
    /// Do a non-blocking call in the background with a callback
    /// </summary>
    /// <param name="request">Call request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="callback">Callback</param>
    /// <param name="options">Call options</param>
    [RequiresUnreferencedCode(SDKConfig.SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SDKConfig.SerializationDynamicCodeMessage)]
    protected abstract void DoCallInBackground(
        JsonRpcClientRequest request,
        CancellationToken cancellationToken,
        Action<JsonRpcClientResult?>? callback,
        ECallOptions options
    );
    /// <summary>
    /// Do an async call
    /// </summary>
    /// <param name="request">Call request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="options">Call options</param>
    /// <returns>The response if the call reached the server</returns>
    [RequiresUnreferencedCode(SDKConfig.SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SDKConfig.SerializationDynamicCodeMessage)]
    protected abstract Task<JsonRpcClientResult?> DoCallAsync(
        JsonRpcClientRequest request,
        CancellationToken cancellationToken,
        ECallOptions options
    );
}
