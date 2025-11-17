using SSC.Misc.Hookable;
using System;

namespace SSC.Net.HttpEx;

/// <summary>
/// Advanced Http server request handler base class
/// </summary>
public interface IHttpServerExRequestHandler
{
    public IHookable<HttpServerExRequestContext> Hooks { get; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try handle the request
    /// </summary>
    /// <param name="context">Request context</param>
    /// <returns>True if the request was handled</returns>
    public bool TryHandle(HttpServerExRequestContext context);
}
