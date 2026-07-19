namespace SSC.DB;

public abstract class IDbModel
{
    public static DbModelMetadata? Metadata { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get database instance attached to this model
    /// </summary>
    /// <returns>DbInstance</returns>
    public static DbInstance GetDbInstance()
        => DbInstance.Get(Metadata!.TableAttribute.DbInstanceName);
}
