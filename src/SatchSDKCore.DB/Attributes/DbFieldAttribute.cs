using System;
using System.Linq;
using System.Reflection;
using SSC.Extensions;

namespace SSC.DB.Attributes;

/// <summary>
///     DB field attribute
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class DbFieldAttribute : Attribute
{
    /// <summary>Gets whether the database generates this field's value.</summary>
    public readonly bool AutoIncrement;
    /// <summary>Gets whether the field participates in the primary key.</summary>
    public readonly bool PrimaryKey;
    /// <summary>Gets whether diagnostic representations redact this field.</summary>
    public readonly bool Sensitive;
    /// <summary>Gets the reflected model field decorated by this attribute.</summary>
    public FieldInfo FieldInfo;
    /// <summary>Gets the database column name.</summary>
    public string FieldName;
    /// <summary>Gets the CLR type of the mapped field.</summary>
    public Type FieldType;
    /// <summary>Gets whether the field is represented as a native enumeration.</summary>
    public bool NativeEnum;
    /// <summary>Gets whether the enumeration is decorated with <see cref="FlagsAttribute" />.</summary>
    public bool NativeFlags;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

#pragma warning disable CS8618
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="fieldName">Name of the field</param>
    /// <param name="primaryKey">Is a primary key?</param>
    /// <param name="autoIncrement">Is an auto increment?</param>
    /// <param name="sensitive">Redact the field value from model string and debugger representations</param>
    public DbFieldAttribute(string? fieldName = null, bool primaryKey = false, bool autoIncrement = false,
        bool sensitive = false)
#pragma warning restore CS8618
    {
        FieldName = fieldName!;
        PrimaryKey = primaryKey;
        AutoIncrement = autoIncrement;
        Sensitive = sensitive;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Init this field attribute
    /// </summary>
    /// <param name="dbModelMetadata">DB model metadata</param>
    /// <param name="fieldInfo">Field reflection info</param>
    /// <exception cref="Exception">If multi auto increment fields are present</exception>
    /// <exception cref="Exception">The auto-increment field type is not primitive.</exception>
    public void Init(DbModelMetadata dbModelMetadata, FieldInfo fieldInfo)
    {
        FieldInfo = fieldInfo;
        FieldType = fieldInfo.FieldType;
        NativeEnum = FieldType.IsEnum && !fieldInfo.CustomAttributes.Any();
        NativeFlags = FieldType.IsEnum && fieldInfo.GetCustomAttribute<FlagsAttribute>() != null;

        if (string.IsNullOrEmpty(FieldName))
        {
            FieldName = fieldInfo.Name.ToSnakeCase();
        }

        if (AutoIncrement)
        {
            if (dbModelMetadata.AutoIncrementField != null)
            {
                throw new Exception($"Model \"{dbModelMetadata.Identifier}\" contains multiple auto incremented field");
            }

            if (!FieldType.IsPrimitive)
            {
                throw new Exception(
                    $"Model \"{dbModelMetadata.Identifier}\", the incremented field need to be a primitive type");
            }

            dbModelMetadata.AutoIncrementField = this;
        }
    }
}
