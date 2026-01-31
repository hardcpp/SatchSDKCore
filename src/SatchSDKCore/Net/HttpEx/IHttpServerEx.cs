using System;
using SSC.Misc.Hookable;

namespace SSC.Net.HttpEx;

/// <summary>
/// Advanced Http Server interface
/// </summary>
public interface IHttpServerEx : IDisposable
{
    IHookable<HttpServerExRequestContext> Hooks { get; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Add a request handler
    /// </summary>
    /// <param name="handler">Handler to add</param>
    void AddRequestHandler(IHttpServerExRequestHandler handler);
    /// <summary>
    /// Remove a request handler
    /// </summary>
    /// <param name="handler">Handler to remove</param>
    void RemoveRequestHandler(IHttpServerExRequestHandler handler);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Start the HttpServerEx server and threads
    /// </summary>
    void Start();
    /// <summary>
    /// Wait for the server
    /// </summary>
    void Wait();
    /// <summary>
    /// Stop the HttpServerEx server and wait for all the threads to stop
    /// </summary>
    void Stop();
}
