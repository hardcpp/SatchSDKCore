namespace SSC.Misc.Hookable;

/// <summary>
/// Hook interface
/// </summary>
/// <typeparam name="T">Context type</typeparam>
public interface IHook<T>
{
    /// <summary>
    /// intercept
    /// </summary>
    /// <param name="context">Context</param>
    /// <returns>True if should interupt context</returns>
    bool Intercept(T context);
}
