using SSC.Extensions;
using System;

namespace SSC.DB.Attributes;

/// <summary>
/// DB table attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class DBTableAttribute : Attribute
{
    public readonly string      DBInstanceName;
    public          string      TableName       { get; private set; }
    public          string?     Schema          { get; private set; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dbInstanceName">Name of the database instance</param>
    /// <param name="tableName">Name of the table</param>
    /// <param name="schema">Name of the schema</param>
    public DBTableAttribute(string dbInstanceName, string? tableName = null, string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(dbInstanceName);

        DBInstanceName  = dbInstanceName;
        TableName       = tableName!;
        Schema          = schema;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Init this table attribute
    /// </summary>
    /// <param name="dbModelMetadata">DB model metadata</param>
    /// <param name="type">Type reflection info</param>
    public void Init(DBModelMetadata dbModelMetadata, Type type)
    {
        if (type.Name.Contains("_"))
            throw new Exception($"Character '_' is not permitted in name of model {type.FullName}");

        if (!type.Name.EndsWith("Model"))
            throw new Exception($"The name of the model {type.FullName} should end with 'Model'");

        if (string.IsNullOrEmpty(TableName))
        {
            var l_ComputedName = type.Name.ToSmakeCase();
            if (l_ComputedName.EndsWith("_model"))
                l_ComputedName = l_ComputedName.Substring(0, l_ComputedName.IndexOf("_model"));

            TableName = l_ComputedName;
        }

        ArgumentNullException.ThrowIfNull(TableName);
    }
}
