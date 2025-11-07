namespace SSC.DB;

public abstract class IDBModel
{
    public static DBModelMetadata? Metadata { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get database instance attached to this model
    /// </summary>
    /// <returns>DBInstance</returns>
    public static DBInstance GetDBInstance()
        => DBInstance.Get(Metadata!.TableAttribute.DBInstanceName);
}
