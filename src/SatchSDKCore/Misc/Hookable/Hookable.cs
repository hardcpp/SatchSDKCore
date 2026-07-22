using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SSC.Misc.Hookable;

/// <summary>
/// Hook interface
/// </summary>
/// <typeparam name="T">Context type</typeparam>
public class Hookable<T> : IHookable<T>
{
    private readonly object     _configurationLock = new();
    private          IHook<T>[] _earlyHooks        = Array.Empty<IHook<T>>();
    private          IHook<T>[] _lateHooks         = Array.Empty<IHook<T>>();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc />
    public void AddEarlyRequestHook(IHook<T> earlyHook)
    {
        ArgumentNullException.ThrowIfNull(earlyHook);

        lock (_configurationLock)
        {
            int existingIdx = Array.IndexOf(_earlyHooks, earlyHook);
            if (existingIdx != -1)
                return;

            var newEarlyHooks = new IHook<T>[_earlyHooks.Length + 1];
            Array.Copy(_earlyHooks, newEarlyHooks, _earlyHooks.Length);
            newEarlyHooks[^1] = earlyHook;

            Volatile.Write(ref _earlyHooks, newEarlyHooks);
        }
    }

    /// <inheritdoc />
    public void RemoveEarlyRequestHook(IHook<T> earlyHook)
    {
        ArgumentNullException.ThrowIfNull(earlyHook);

        lock (_configurationLock)
        {
            IHook<T>[] oldEarlyHooks = _earlyHooks;
            int        existingIdx   = Array.IndexOf(oldEarlyHooks, earlyHook);
            if (existingIdx == -1)
                return;

            var newEarlyHooks = new IHook<T>[oldEarlyHooks.Length - 1];
            Array.Copy(oldEarlyHooks, 0, newEarlyHooks, 0, existingIdx);
            Array.Copy(oldEarlyHooks, existingIdx + 1, newEarlyHooks, existingIdx,
                       oldEarlyHooks.Length       - existingIdx - 1);

            Volatile.Write(ref _earlyHooks, newEarlyHooks);
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc />
    public void AddLateRequestHook(IHook<T> lateHook)
    {
        ArgumentNullException.ThrowIfNull(lateHook);

        lock (_configurationLock)
        {
            int existingIdx = Array.IndexOf(_lateHooks, lateHook);
            if (existingIdx != -1)
                return;

            var newLateHooks = new IHook<T>[_lateHooks.Length + 1];
            Array.Copy(_lateHooks, newLateHooks, _lateHooks.Length);
            newLateHooks[^1] = lateHook;

            Volatile.Write(ref _lateHooks, newLateHooks);
        }
    }

    /// <inheritdoc />
    public void RemoveLateRequestHook(IHook<T> lateHook)
    {
        ArgumentNullException.ThrowIfNull(lateHook);

        lock (_configurationLock)
        {
            IHook<T>[] oldLateHooks = _lateHooks;
            int        existingIdx  = Array.IndexOf(oldLateHooks, lateHook);
            if (existingIdx == -1)
                return;

            var newLateHooks = new IHook<T>[oldLateHooks.Length - 1];
            Array.Copy(oldLateHooks, 0,               newLateHooks, 0,           existingIdx);
            Array.Copy(oldLateHooks, existingIdx + 1, newLateHooks, existingIdx, oldLateHooks.Length - existingIdx - 1);

            Volatile.Write(ref _lateHooks, newLateHooks);
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <inheritdoc />
    public bool InterceptEarly(T context)
        => InterceptImpl(Volatile.Read(ref _earlyHooks), context);

    /// <inheritdoc />
    public bool InterceptLate(T context)
        => InterceptImpl(Volatile.Read(ref _lateHooks), context);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try intercept implementations
    /// </summary>
    /// <param name="hooks">Hook array</param>
    /// <param name="context">Current context</param>
    /// <returns>True if should interupt context</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool InterceptImpl(IHook<T>[] hooks, T context)
    {
        for (int i = 0; i < hooks.Length; i++)
        {
            if (!hooks[i].Intercept(context))
                continue;

            return true;
        }

        return false;
    }
}
