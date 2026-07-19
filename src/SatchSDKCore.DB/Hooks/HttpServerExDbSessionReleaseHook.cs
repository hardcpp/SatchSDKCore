using SSC.Misc.Hookable;
using SSC.Net.HttpEx;

namespace SSC.DB.Hooks;

/// <summary>
/// Hook for HTTPServer that force the release of any IDbSession open
/// </summary>
public class HttpServerExDbSessionReleaseHook : Net.HttpEx.IHttpServerExRequestHandler
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
        for (int i = 0; i < DbInstance.Instances.Length; ++i)
        {
            var existingSession = DbInstance.Instances[i].GetTlsSession(skipAcquire: true);
            if (existingSession != null)
                existingSession.DisposeFinal(force: true);
        }

        return false;
    }
}
