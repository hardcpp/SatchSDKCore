using SSC.Misc.Hookable;
using System;

namespace SSC.Net.HttpEx;

/// <summary>
/// Advanced Http Server interface
/// </summary>
public interface IHttpServerEx : IDisposable
{
    public IHookable<HttpServerExRequestContext> Hooks { get; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Add a request handler
    /// </summary>
    /// <param name="handler">Handler to add</param>
    public void AddRequestHandler(IHttpServerExRequestHandler handler);
    /// <summary>
    /// Remove a request handler
    /// </summary>
    /// <param name="handler">Handler to remove</param>
    public void RemoveRequestHandler(IHttpServerExRequestHandler handler);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Start the HttpServerEx server and threads
    /// </summary>
    public void Start();
    /// <summary>
    /// Wait for the server
    /// </summary>
    public void Wait();
    /// <summary>
    /// Stop the HttpServerEx server and wait for all the threads to stop
    /// </summary>
    public void Stop();
}