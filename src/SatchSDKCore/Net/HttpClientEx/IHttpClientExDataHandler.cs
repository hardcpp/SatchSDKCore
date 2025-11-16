using System;
using System.Threading.Tasks;

namespace SSC.Net.HttpClientEx;

/// <summary>
/// Advanced Http client data handler interface
/// </summary>
public interface IHttpClientExDataHandler
{
    public abstract int IdealBufferSize { get; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// When the reading begin
    /// </summary>
    public abstract void Begin();
    /// <summary>
    /// Process a buffer
    /// </summary>
    /// <param name="buffer">Current buffer</param>
    /// <param name="totalSize">Total size if known</param>
    /// <returns></returns>
    public abstract ValueTask ProcessAsync(ReadOnlySpan<byte> buffer, long? totalSize);
    /// <summary>
    /// When the reading end
    /// </summary>
    public abstract void End();
}
