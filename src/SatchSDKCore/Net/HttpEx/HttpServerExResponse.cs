using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace SSC.Net.HttpEx;

/// <summary>
/// Advanced Http Server response
/// </summary>
public class HttpServerExResponse
{
    public readonly HttpStatusCode Code;
    public readonly HttpContent?   Content;
    public readonly Encoding?      ContentEncoding;
    public readonly string?        ContentType;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="code">Response code</param>
    /// <param name="content">Content</param>
    /// <param name="contentEncoding">Optional encoding</param>
    public HttpServerExResponse(HttpStatusCode code, HttpContent? content, Encoding? contentEncoding)
    {
        Code            = code;
        Content         = content;
        ContentEncoding = contentEncoding;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try write the response to an HttpListenerResponse
    /// </summary>
    /// <param name="httpResponse">Target</param>
    /// <param name="outError">Output error if any</param>
    /// <returns>True if success</returns>
    public bool TryWrite(HttpListenerResponse httpResponse, out string? outError)
    {
        outError = null;

        if (httpResponse == null)
        {
            outError = "No valid HttpListenerReponse to write to";
            return false;
        }

        if (!httpResponse.OutputStream.CanWrite)
        {
            outError = "Output stream in HttpListenerReponse cannot be write to";
            return false;
        }

        httpResponse.StatusCode = (int)Code;

        if (Content != null)
        {
            httpResponse.Headers.Set("Content-Type", Content.Headers.ContentType?.MediaType);
            if (ContentEncoding != null)
            {
                httpResponse.ContentEncoding = ContentEncoding;
                if (ContentEncoding == Encoding.UTF8)
                    httpResponse.Headers.Set("Content-Type", $"{Content.Headers.ContentType?.MediaType}; charset=utf-8");
            }

            if (Content.Headers.ContentLength.HasValue)
                httpResponse.ContentLength64 = Content.Headers.ContentLength.Value;

            Content.CopyTo(httpResponse.OutputStream, null, CancellationToken.None);
        }

        return true;
    }
}
