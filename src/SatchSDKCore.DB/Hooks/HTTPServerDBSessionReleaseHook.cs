namespace SSC.DB.Hooks;

/// <summary>
/// Hook for HTTPServer that force the release of any DBSession open
/// </summary>
public class HTTPServerDBSessionReleaseHook : Network.HTTP.IHTTPServerRequestHook
{
    /// <summary>
    /// Try intercept the request
    /// </summary>
    /// <param name="context">Request context</param>
    /// <returns>True if the request handling should be interrupted</returns>
    public bool TryIntercept(Network.HTTP.HTTPServerRequestContext context)
    {
        for (int l_I = 0; l_I < DBInstance.Instances.Length; ++l_I)
        {
            var l_ExistingSession = DBInstance.Instances[l_I].GetTLSSession(skipAcquire: true);
            if (l_ExistingSession != null)
                l_ExistingSession.Dispose(force: true);
        }

        return false;
    }
}
