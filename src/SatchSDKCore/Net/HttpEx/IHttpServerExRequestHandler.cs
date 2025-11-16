using System;

namespace SSC.Net.HttpEx;

/// <summary>
/// Advanced Http server request handler base class
/// </summary>
public abstract class IHttpServerExRequestHandler
{
    private IHttpServerExRequestHook[] m_EarlyHooks = Array.Empty<IHttpServerExRequestHook>();
    private IHttpServerExRequestHook[] m_LateHooks  = Array.Empty<IHttpServerExRequestHook>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Add a early request Hook
    /// </summary>
    /// <param name="earlyHook">Early hook to add</param>
    public void AddEarlyRequestHook(IHttpServerExRequestHook earlyHook)
    {
        ArgumentNullException.ThrowIfNull(earlyHook);

        var l_ExistingIdx = Array.IndexOf(m_EarlyHooks, earlyHook);
        if (l_ExistingIdx != -1)
            return;

        var l_NewEarlyHooks = new IHttpServerExRequestHook[m_EarlyHooks.Length + 1];
        Array.Copy(m_EarlyHooks, l_NewEarlyHooks, m_EarlyHooks.Length);
        l_NewEarlyHooks[^1] = earlyHook;

        m_EarlyHooks = l_NewEarlyHooks;
    }
    /// <summary>
    /// Remove a early request Hook
    /// </summary>
    /// <param name="earlyHook">Early hook to remove</param>
    public void RemoveEarlyRequestHook(IHttpServerExRequestHook earlyHook)
    {
        ArgumentNullException.ThrowIfNull(earlyHook);

        var l_OldEarlyHooks = m_EarlyHooks;
        var l_ExistingIdx = Array.IndexOf(l_OldEarlyHooks, earlyHook);
        if (l_ExistingIdx == -1)
            return;

        var l_NewEarlyHooks = new IHttpServerExRequestHook[l_OldEarlyHooks.Length - 1];
        Array.Copy(l_OldEarlyHooks, 0, l_NewEarlyHooks, 0, l_ExistingIdx);
        Array.Copy(l_OldEarlyHooks, l_ExistingIdx + 1, l_NewEarlyHooks, l_ExistingIdx, l_OldEarlyHooks.Length - l_ExistingIdx - 1);

        m_EarlyHooks = l_NewEarlyHooks;
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

        var l_ExistingIdx = Array.IndexOf(m_LateHooks, lateHook);
        if (l_ExistingIdx != -1)
            return;

        var l_NewLateHooks = new IHttpServerExRequestHook[m_LateHooks.Length + 1];
        Array.Copy(m_LateHooks, l_NewLateHooks, m_LateHooks.Length);
        l_NewLateHooks[^1] = lateHook;

        m_LateHooks = l_NewLateHooks;
    }
    /// <summary>
    /// Remove a late request Hook
    /// </summary>
    /// <param name="lateHook">Late hook to remove</param>
    public void RemoveLateRequestHook(IHttpServerExRequestHook lateHook)
    {
        ArgumentNullException.ThrowIfNull(lateHook);

        var l_OldLateHooks = m_LateHooks;
        var l_ExistingIdx = Array.IndexOf(l_OldLateHooks, lateHook);
        if (l_ExistingIdx == -1)
            return;

        var l_NewLateHooks = new IHttpServerExRequestHook[l_OldLateHooks.Length - 1];
        Array.Copy(l_OldLateHooks, 0, l_NewLateHooks, 0, l_ExistingIdx);
        Array.Copy(l_OldLateHooks, l_ExistingIdx + 1, l_NewLateHooks, l_ExistingIdx, l_OldLateHooks.Length - l_ExistingIdx - 1);

        m_LateHooks = l_NewLateHooks;
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
        var l_EarlyHooks = m_EarlyHooks;
        for (var l_I = 0; l_I < l_EarlyHooks.Length; l_I++)
        {
            if (!l_EarlyHooks[l_I].TryIntercept(context))
                continue;

            return true;
        }

        var l_Result = TryHandleImplementation(context);

        var l_LateHooks = m_LateHooks;
        for (var l_I = 0; l_I < l_LateHooks.Length; l_I++)
        {
            if (!l_LateHooks[l_I].TryIntercept(context))
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
