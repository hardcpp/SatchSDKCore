using System;
using SSC.Extensions;

namespace SSC.DB.Attributes;

/// <summary>
/// DB table attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class DbTableAttribute : Attribute
{
    /// <summary>Gets the name of the database instance used by the model.</summary>
    public readonly string DbInstanceName;
    /// <summary>Gets the mapped database table name.</summary>
    public string TableName { get; private set; }
    /// <summary>Gets the optional database schema name.</summary>
    public string? Schema { get; private set; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dbInstanceName">Name of the database instance.</param>
    /// <param name="tableName">Optional database table name; inferred from the model type when omitted.</param>
    /// <param name="schema">Optional database schema name.</param>
    public DbTableAttribute(string dbInstanceName, string? tableName = null, string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(dbInstanceName);

        DbInstanceName = dbInstanceName;
        TableName = tableName!;
        Schema = schema;
    }
    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Init this table attribute
    /// </summary>
    /// <param name="dbModelMetadata">DB model metadata</param>
    /// <param name="modelInfo">Type reflection info</param>
    public void Init(DbModelMetadata dbModelMetadata, Type modelInfo)
    {
        if (modelInfo.Name.Contains('_'))
        {
            throw new Exception($"Character '_' is not permitted in name of model {modelInfo.FullName}");
        }

        if (!modelInfo.Name.EndsWith("Model"))
        {
            throw new Exception($"The name of the model {modelInfo.FullName} should end with 'Model'");
        }

        if (string.IsNullOrEmpty(TableName))
        {
            string computedName = modelInfo.Name.ToSnakeCase();
            if (computedName.EndsWith("_model"))
            {
                computedName = computedName.Substring(0, computedName.IndexOf("_model", StringComparison.Ordinal));
            }

            TableName = computedName;
        }

        ArgumentNullException.ThrowIfNull(TableName);
    }
}
