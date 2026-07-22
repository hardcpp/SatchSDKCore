using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace SSC.Net.JsonRpc;

/// <summary>
/// JsonRpc client interface
/// </summary>
public interface IJsonRpcClient
{
    /// <summary>
    /// Options for calls
    /// </summary>
    [Flags]
    enum ECallOptions
    {
        None              = 0,
        IgnoreRetryPolicy = 1 << 0
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Do a sync call
    /// </summary>
    /// <param name="method">Method to call</param>
    /// <param name="parameters">Method parameters</param>
    /// <param name="options">Call options</param>
    /// <returns>The response if the call reached the server</returns>
    [RequiresUnreferencedCode(SDKConfig.SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SDKConfig.SerializationDynamicCodeMessage)]
    JsonRpcClientResult? Call(
        string              method,
        IEnumerable<object> parameters,
        ECallOptions        options = ECallOptions.None
    );

    /// <summary>
    /// Do a sync call
    /// </summary>
    /// <param name="method">Method to call</param>
    /// <param name="parameters">Method parameters</param>
    /// <param name="options">Call options</param>
    /// <returns>The response if the call reached the server</returns>
    [RequiresUnreferencedCode(SDKConfig.SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SDKConfig.SerializationDynamicCodeMessage)]
    JsonRpcClientResult? Call(
        string                              method,
        IReadOnlyDictionary<string, object> parameters,
        ECallOptions                        options = ECallOptions.None
    );

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
    void CallInBackground(
        string                        method,
        IEnumerable<object>           parameters,
        CancellationToken             cancellationToken,
        Action<JsonRpcClientResult?>? callback,
        ECallOptions                  options = ECallOptions.None
    );

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
    void CallInBackground(
        string                              method,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken                   cancellationToken,
        Action<JsonRpcClientResult?>?       callback,
        ECallOptions                        options = ECallOptions.None
    );

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
    Task<JsonRpcClientResult?> CallAsync(
        string              method,
        IEnumerable<object> parameters,
        CancellationToken   cancellationToken,
        ECallOptions        options = ECallOptions.None
    );

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
    Task<JsonRpcClientResult?> CallAsync(
        string                              method,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken                   cancellationToken,
        ECallOptions                        options = ECallOptions.None
    );
}
