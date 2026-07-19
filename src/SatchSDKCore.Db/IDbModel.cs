namespace SSC.Db;

/// <summary>
/// Defines persistence operations and metadata exposed by a database model.
/// </summary>
public interface IDbModel
{
    /// <summary>Gets the mapping metadata for the implementing model type.</summary>
    static abstract DbModelMetadata Metadata { get; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get the database instance attached to this model.
    /// </summary>
    static abstract DbInstance GetDbInstance();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Insert this model and populate its auto-increment field, when present.
    /// </summary>
    /// <param name="dbSession">An optional caller-owned session.</param>
    /// <returns>The number of inserted rows.</returns>
    int Insert(IDbSession? dbSession = null);

    /// <summary>
    /// Update all mutable fields, identifying this model by all of its primary keys.
    /// </summary>
    /// <param name="dbSession">An optional caller-owned session.</param>
    /// <returns>The number of updated rows.</returns>
    int Update(IDbSession? dbSession = null);

    /// <summary>
    /// Delete this model, identifying it by all of its primary keys.
    /// </summary>
    /// <param name="dbSession">An optional caller-owned session.</param>
    /// <returns>The number of deleted rows.</returns>
    int Delete(IDbSession? dbSession = null);
}
