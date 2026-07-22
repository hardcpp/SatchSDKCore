namespace SSC.Misc;

/// <summary>
/// Configuration that can be made immutable before concurrent use.
/// </summary>
public interface IFreezable
{
    bool IsFrozen { get; }

    /// <summary>
    /// Freeze this freezable object
    /// </summary>
    void Freeze();
}
