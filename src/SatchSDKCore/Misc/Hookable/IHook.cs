namespace SSC.Misc.Hookable;

/// <summary>
/// Hook interface
/// </summary>
/// <typeparam name="T">Context type</typeparam>
public interface IHook<T>
{
    /// <summary>
    /// Intercept
    /// </summary>
    /// <param name="context">Context</param>
    /// <returns>True if it should interrupt context</returns>
    bool Intercept(T context);
}
