namespace SSC.Misc.Hookable;

/// <summary>
/// Hookable interface
/// </summary>
/// <typeparam name="T">Context type</typeparam>
public interface IHookable<T>
{
    /// <summary>
    /// Add a early Hook
    /// </summary>
    /// <param name="earlyHook">Early hook to add</param>
    public void AddEarlyRequestHook(IHook<T> earlyHook);
    /// <summary>
    /// Remove a early Hook
    /// </summary>
    /// <param name="earlyHook">Early hook to remove</param>
    public void RemoveEarlyRequestHook(IHook<T> earlyHook);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Add a late Hook
    /// </summary>
    /// <param name="lateHook">Late hook to add</param>
    public void AddLateRequestHook(IHook<T> lateHook);
    /// <summary>
    /// Remove a late Hook
    /// </summary>
    /// <param name="lateHook">Late hook to remove</param>
    public void RemoveLateRequestHook(IHook<T> lateHook);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try to run early hooks against the context
    /// </summary>
    /// <param name="context">Current context</param>
    /// <returns>True if should interupt context</returns>
    public bool InterceptEarly(T context);
    /// <summary>
    /// Try to run late hooks against the context
    /// </summary>
    /// <param name="context">Current context</param>
    /// <returns>True if should interupt context</returns>
    public bool InterceptLate(T context);
}
