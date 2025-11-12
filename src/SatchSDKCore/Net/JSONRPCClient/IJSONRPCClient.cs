using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SSC.Net.JSONRPCClient;

public abstract class IJSONRPCClient
{
    internal const string SerializationUnreferencedCodeMessage = "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.";
    internal const string SerializationDynamicCodeMessage = "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.";

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Options for calls
    /// </summary>
    [Flags]
    public enum ECallOptions
    {
        None              = 0,
        IgnoreRetryPolicy = 1 << 0,
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
    [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SerializationDynamicCodeMessage)]
    public JSONRPCClientResult? Call(
        string              method,
        IEnumerable<object> parameters,
        ECallOptions        options     = ECallOptions.None
    )
    {
        var request = new JSONRPCClientRequest
        {
            Method = method,
            Params = JsonSerializer.SerializeToElement(parameters)
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
    [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SerializationDynamicCodeMessage)]
    public JSONRPCClientResult? Call(
        string                              method,
        IReadOnlyDictionary<string, object> parameters,
        ECallOptions                        options     = ECallOptions.None
    )
    {
        var request = new JSONRPCClientRequest
        {
            Method = method,
            Params = JsonSerializer.SerializeToElement(parameters)
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
    [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SerializationDynamicCodeMessage)]
    public void CallInBackground(
        string                  method,
        IEnumerable<object>     parameters,
        CancellationToken       cancellationToken,
        Action<JSONRPCClientResult?>? callback,
        ECallOptions            options           = ECallOptions.None
    )
    {
        var request = new JSONRPCClientRequest
        {
            Method = method,
            Params = JsonSerializer.SerializeToElement(parameters)
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
    [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SerializationDynamicCodeMessage)]
    public void CallInBackground(
        string                              method,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken                   cancellationToken,
        Action<JSONRPCClientResult?>?             callback,
        ECallOptions                        options           = ECallOptions.None
    )
    {
        var request = new JSONRPCClientRequest
        {
            Method = method,
            Params = JsonSerializer.SerializeToElement(parameters)
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
    [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SerializationDynamicCodeMessage)]
    public Task<JSONRPCClientResult?> CallAsync(
        string              method,
        IEnumerable<object> parameters,
        CancellationToken   cancellationToken,
        ECallOptions        options           = ECallOptions.None
    )
    {
        var request = new JSONRPCClientRequest
        {
            Method = method,
            Params = JsonSerializer.SerializeToElement(parameters)
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
    [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SerializationDynamicCodeMessage)]
    public Task<JSONRPCClientResult?> CallAsync(
        string                              method,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken                   cancellationToken,
        ECallOptions                        options           = ECallOptions.None
    )
    {
        var request = new JSONRPCClientRequest
        {
            Method = method,
            Params = JsonSerializer.SerializeToElement(parameters)
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
    [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SerializationDynamicCodeMessage)]
    protected abstract JSONRPCClientResult? DoCall(
        JSONRPCClientRequest request,
        ECallOptions         options
    );
    /// <summary>
    /// Do a non-blocking call in the background with a callback
    /// </summary>
    /// <param name="request">Call request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="callback">Callback</param>
    /// <param name="options">Call options</param>
    [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SerializationDynamicCodeMessage)]
    protected abstract void DoCallInBackground(
        JSONRPCClientRequest    request,
        CancellationToken       cancellationToken,
        Action<JSONRPCClientResult?>? callback,
        ECallOptions            options
    );
    /// <summary>
    /// Do an async call
    /// </summary>
    /// <param name="request">Call request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="options">Call options</param>
    /// <returns>The response if the call reached the server</returns>
    [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(SerializationDynamicCodeMessage)]
    protected abstract Task<JSONRPCClientResult?> DoCallAsync(
        JSONRPCClientRequest request,
        CancellationToken    cancellationToken,
        ECallOptions         options
    );
}
