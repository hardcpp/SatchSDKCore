using SSC.Extensions;
using System;

namespace SSC.DB.Attributes;

/// <summary>
/// DB table attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class DbTableAttribute : Attribute
{
    public readonly string DbInstanceName;
    public string TableName { get; private set; }
    public string? Schema { get; private set; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dbInstanceName">Name of the database instance</param>
    /// <param name="tableName">Name of the table</param>
    /// <param name="schema">Name of the schema</param>
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
    /// <param name="type">Type reflection info</param>
    public void Init(DbModelMetadata dbModelMetadata, Type type)
    {
        if (type.Name.Contains('_'))
            throw new Exception($"Character '_' is not permitted in name of model {type.FullName}");

        if (!type.Name.EndsWith("Model"))
            throw new Exception($"The name of the model {type.FullName} should end with 'Model'");

        if (string.IsNullOrEmpty(TableName))
        {
            var computedName = type.Name.ToSnakeCase();
            if (computedName.EndsWith("_model"))
                computedName = computedName.Substring(0, computedName.IndexOf("_model"));

            TableName = computedName;
        }

        ArgumentNullException.ThrowIfNull(TableName);
    }
}
