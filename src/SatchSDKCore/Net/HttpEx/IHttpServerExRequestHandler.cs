using System;

namespace SSC.Net.HttpEx;

/// <summary>
/// Advanced Http server request handler base class
/// </summary>
public abstract class IHttpServerExRequestHandler
{
    private IHttpServerExRequestHook[] _earlyHooks = Array.Empty<IHttpServerExRequestHook>();
    private IHttpServerExRequestHook[] _lateHooks  = Array.Empty<IHttpServerExRequestHook>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Add a early request Hook
    /// </summary>
    /// <param name="earlyHook">Early hook to add</param>
    public void AddEarlyRequestHook(IHttpServerExRequestHook earlyHook)
    {
        ArgumentNullException.ThrowIfNull(earlyHook);

        var existingIdx = Array.IndexOf(_earlyHooks, earlyHook);
        if (existingIdx != -1)
            return;

        var newEarlyHooks = new IHttpServerExRequestHook[_earlyHooks.Length + 1];
        Array.Copy(_earlyHooks, newEarlyHooks, _earlyHooks.Length);
        newEarlyHooks[^1] = earlyHook;

        _earlyHooks = newEarlyHooks;
    }
    /// <summary>
    /// Remove a early request Hook
    /// </summary>
    /// <param name="earlyHook">Early hook to remove</param>
    public void RemoveEarlyRequestHook(IHttpServerExRequestHook earlyHook)
    {
        ArgumentNullException.ThrowIfNull(earlyHook);

        var oldEarlyHooks = _earlyHooks;
        var existingIdx = Array.IndexOf(oldEarlyHooks, earlyHook);
        if (existingIdx == -1)
            return;

        var newEarlyHooks = new IHttpServerExRequestHook[oldEarlyHooks.Length - 1];
        Array.Copy(oldEarlyHooks,               0, newEarlyHooks,           0,                            existingIdx);
        Array.Copy(oldEarlyHooks, existingIdx + 1, newEarlyHooks, existingIdx, oldEarlyHooks.Length - existingIdx - 1);

        _earlyHooks = newEarlyHooks;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Add a late request Hook
    /// </summary>
    /// <param name="lateHook">Late hook to add</param>
    public void AddLateRequestHook(IHttpServerExRequestHook lateHook)
    {
        ArgumentNullException.ThrowIfNull(lateHook);

        var existingIdx = Array.IndexOf(_lateHooks, lateHook);
        if (existingIdx != -1)
            return;

        var newLateHooks = new IHttpServerExRequestHook[_lateHooks.Length + 1];
        Array.Copy(_lateHooks, newLateHooks, _lateHooks.Length);
        newLateHooks[^1] = lateHook;

        _lateHooks = newLateHooks;
    }
    /// <summary>
    /// Remove a late request Hook
    /// </summary>
    /// <param name="lateHook">Late hook to remove</param>
    public void RemoveLateRequestHook(IHttpServerExRequestHook lateHook)
    {
        ArgumentNullException.ThrowIfNull(lateHook);

        var oldLateHooks = _lateHooks;
        var existingIdx = Array.IndexOf(oldLateHooks, lateHook);
        if (existingIdx == -1)
            return;

        var newLateHooks = new IHttpServerExRequestHook[oldLateHooks.Length - 1];
        Array.Copy(oldLateHooks,               0, newLateHooks,           0,                           existingIdx);
        Array.Copy(oldLateHooks, existingIdx + 1, newLateHooks, existingIdx, oldLateHooks.Length - existingIdx - 1);

        _lateHooks = newLateHooks;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try handle the request
    /// </summary>
    /// <param name="context">Request context</param>
    /// <returns>True if the request was handled</returns>
    public bool TryHandle(HttpServerExRequestContext context)
    {
        var earlyHooks = _earlyHooks;
        for (var i = 0; i < earlyHooks.Length; i++)
        {
            if (!earlyHooks[i].TryIntercept(context))
                continue;

            return true;
        }

        var l_Result = TryHandleImplementation(context);

        var lateHooks = _lateHooks;
        for (var i = 0; i < lateHooks.Length; i++)
        {
            if (!lateHooks[i].TryIntercept(context))
                continue;

            return true;
        }

        return l_Result;
    }
    /// <summary>
    /// Try handle the request
    /// </summary>
    /// <param name="context">Request context</param>
    /// <returns>True if the request was handled</returns>
    protected abstract bool TryHandleImplementation(HttpServerExRequestContext context);
}
