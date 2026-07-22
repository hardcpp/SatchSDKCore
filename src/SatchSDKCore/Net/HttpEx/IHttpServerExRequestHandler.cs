using System.Threading;
using System.Threading.Tasks;
using SSC.Misc.Hookable;

namespace SSC.Net.HttpEx;

/// <summary>
/// Advanced Http server request handler base class
/// </summary>
public interface IHttpServerExRequestHandler
{
    IHookable<HttpServerExRequestContext> Hooks { get; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try handle the request
    /// </summary>
    /// <param name="context">Request context</param>
    /// <returns>True if the request was handled</returns>
    ValueTask<bool> TryHandleAsync(
        HttpServerExRequestContext context,
        CancellationToken          cancellationToken = default);
}
