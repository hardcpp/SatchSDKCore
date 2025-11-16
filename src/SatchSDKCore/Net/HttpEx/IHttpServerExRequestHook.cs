namespace SSC.Net.HttpEx;

/// <summary>
/// Advanced Http server request hook
/// </summary>
public interface IHttpServerExRequestHook
{
    /// <summary>
    /// Try intercept the request
    /// </summary>
    /// <param name="context">Request context</param>
    /// <returns>True if the request handling should be interrupted</returns>
    public abstract bool TryIntercept(HttpServerExRequestContext context);
}
