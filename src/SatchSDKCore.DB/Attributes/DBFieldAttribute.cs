using System;
using System.Linq;
using System.Reflection;

namespace SSC.DB.Attributes;

/// <summary>
/// DB field attribute
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
public class DBFieldAttribute : Attribute
{
    public          FieldInfo   FieldInfo;
    public          Type        FieldType;
    public          string      Name;
    public readonly bool        PrimaryKey;
    public readonly bool        AutoIncrement;
    public          bool        NativeEnum;
    public          bool        NativeFlags;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

#pragma warning disable CS8618
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="primaryKey">Is a primary key?</param>
    /// <param name="autoIncrement">Is an auto increment?</param>
    public DBFieldAttribute(bool primaryKey = false, bool autoIncrement = false)
#pragma warning restore CS8618
    {
        PrimaryKey      = primaryKey;
        AutoIncrement   = autoIncrement;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Init this field attribute
    /// </summary>
    /// <param name="dbModelMetadata">DB model metadata</param>
    /// <param name="fieldInfo">Field reflection info</param>
    /// <exception cref="Exception">If multi auto increment fields are present</exception>
    /// <exception cref="Exception">If the field type for autoincrement is not primitive</exception>
    public void Init(DBModelMetadata dbModelMetadata, FieldInfo fieldInfo)
    {
        FieldInfo   = fieldInfo;
        FieldType   = fieldInfo.FieldType;
        Name        = fieldInfo.Name;
        NativeEnum  = FieldType.IsEnum && fieldInfo.CustomAttributes.Count() == 0;
        NativeFlags = FieldType.IsEnum && fieldInfo.GetCustomAttribute<FlagsAttribute>() != null;

        if (AutoIncrement == true)
        {
            if (dbModelMetadata.AutoIncrementField != null)
                throw new Exception($"Model \"{dbModelMetadata.Identifier}\" contains multiple auto incremented field");

            if (!FieldType.IsPrimitive)
                throw new Exception($"Model \"{dbModelMetadata.Identifier}\", the incremented field need to be a primitive type");

            dbModelMetadata.AutoIncrementField = this;
        }
    }
}
