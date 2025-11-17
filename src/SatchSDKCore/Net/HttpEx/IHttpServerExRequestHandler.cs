using SSC.Misc.Hookable;
using System;

namespace SSC.Net.HttpEx;

/// <summary>
/// Advanced Http server request handler base class
/// </summary>
public abstract class IHttpServerExRequestHandler
{
    public IHookable<HttpServerExRequestContext> Hooks { get; } = new Hookable<HttpServerExRequestContext>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try handle the request
    /// </summary>
    /// <param name="context">Request context</param>
    /// <returns>True if the request was handled</returns>
    public bool TryHandle(HttpServerExRequestContext context)
    {
        if (Hooks.InterceptEarly(context))
            return true;

        var l_Result = TryHandleImplementation(context);

        if (Hooks.InterceptLate(context))
            return true;

        return l_Result;
    }
    /// <summary>
    /// Try handle the request
    /// </summary>
    /// <param name="context">Request context</param>
    /// <returns>True if the request was handled</returns>
    protected abstract bool TryHandleImplementation(HttpServerExRequestContext context);
}
