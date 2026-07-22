using System;
using System.Threading.Tasks;

namespace SSC.Net.HttpEx;

/// <summary>
/// Advanced Http client data handler interface
/// </summary>
public interface IHttpClientExDataHandler
{
    int IdealBufferSize { get; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// When the reading begin
    /// </summary>
    void Begin();

    /// <summary>
    /// Process a buffer
    /// </summary>
    /// <param name="buffer">Current buffer</param>
    /// <param name="totalSize">Total size if known</param>
    /// <returns></returns>
    ValueTask ProcessAsync(ReadOnlySpan<byte> buffer, long? totalSize);

    /// <summary>
    /// When the reading end
    /// </summary>
    void End();
}
