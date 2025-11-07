namespace SSC.Network.HTTP;

public interface IHTTPServerRequestHook
{
    /// <summary>
    /// Try intercept the request
    /// </summary>
    /// <param name="context">Request context</param>
    /// <returns>True if the request handling should be interrupted</returns>
    public abstract bool TryIntercept(HTTPServerRequestContext context);
}
