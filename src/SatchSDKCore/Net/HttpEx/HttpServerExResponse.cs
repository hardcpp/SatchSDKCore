using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace SSC.Net.HttpEx;

/// <summary>
/// Advanced HTTP server response.
/// </summary>
public class HttpServerExResponse
{
    public readonly HttpStatusCode Code;
    public readonly HttpContent? Content;
    public readonly Encoding? ContentEncoding;
    public readonly string? ContentType;

    /// <summary>
    /// Additional HTTP response headers.
    /// </summary>
    public readonly IReadOnlyDictionary<string, string>? Headers;

    /// <summary>
    /// Preserve content headers, including Content-Length, but do not
    /// write the response body. This is used for HEAD responses.
    /// </summary>
    public readonly bool SuppressBody;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor.
    /// </summary>
    public HttpServerExResponse(
        HttpStatusCode code,
        HttpContent? content,
        Encoding? contentEncoding,
        IReadOnlyDictionary<string, string>? headers = null,
        bool suppressBody = false)
    {
        Code = code;
        Content = content;
        ContentEncoding = contentEncoding;
        Headers = headers;
        SuppressBody = suppressBody;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Create a response with the same status, content and headers,
    /// but without writing the response body.
    /// </summary>
    public HttpServerExResponse WithSuppressedBody()
    {
        if (SuppressBody)
            return this;

        return new HttpServerExResponse(
            Code,
            Content,
            ContentEncoding,
            Headers,
            suppressBody: true);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try to write the response to an HttpListenerResponse.
    /// </summary>
    public bool TryWrite(
        HttpListenerResponse httpResponse,
        out string? outError)
    {
        outError = null;

        if (httpResponse == null)
        {
            outError = "No valid HttpListenerReponse to write to";
            return false;
        }

        if (!httpResponse.OutputStream.CanWrite)
        {
            outError =
                "Output stream in HttpListenerReponse cannot be write to";

            return false;
        }

        httpResponse.StatusCode = (int)Code;

        if (Headers != null)
        {
            foreach (var header in Headers)
                httpResponse.Headers.Set(header.Key, header.Value);
        }

        if (Content == null)
            return true;

        var mediaType = Content.Headers.ContentType?.MediaType;

        if (!string.IsNullOrEmpty(mediaType))
            httpResponse.Headers.Set("Content-Type", mediaType);

        if (ContentEncoding != null)
        {
            httpResponse.ContentEncoding = ContentEncoding;

            if (ContentEncoding == Encoding.UTF8 &&
                !string.IsNullOrEmpty(mediaType))
            {
                httpResponse.Headers.Set(
                    "Content-Type",
                    $"{mediaType}; charset=utf-8");
            }
        }

        // HEAD should retain the content length that GET would have
        // returned, while omitting the actual body.
        if (Content.Headers.ContentLength.HasValue)
        {
            httpResponse.ContentLength64 =
                Content.Headers.ContentLength.Value;
        }

        if (!SuppressBody)
        {
            Content.CopyTo(
                httpResponse.OutputStream,
                null,
                CancellationToken.None);
        }

        return true;
    }
}
