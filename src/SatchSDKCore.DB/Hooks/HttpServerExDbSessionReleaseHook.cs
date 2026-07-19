using SSC.Misc.Hookable;
using SSC.Net.HttpEx;

namespace SSC.DB.Hooks;

/// <summary>
/// Hook for HTTPServer that force the release of any IDbSession open
/// </summary>
public class HttpServerExDbSessionReleaseHook : IHook<HttpServerExRequestContext>
{
    /// <inheritdoc />
    public bool Intercept(HttpServerExRequestContext context)
    {
        for (int i = 0; i < DbInstance.Instances.Length; ++i)
        {
            IDbSession? existingSession = DbInstance.Instances[i].GetTlsSession(true);
            if (existingSession != null)
            {
                existingSession.DisposeFinal(true);
            }
        }

        return false;
    }
}
