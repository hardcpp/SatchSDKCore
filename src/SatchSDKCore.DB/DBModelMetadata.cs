using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace SSC.DB;

/// <summary>
/// DB Model metadata
/// </summary>
public class DBModelMetadata
{
    private static Dictionary<Type, DBModelMetadata> s_Cache = new();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get from cache of create DBModelMetadata for a type
    /// </summary>
    /// <typeparam name="t_Type">Type of the DBModel</typeparam>
    /// <returns>DBModelMetadata</returns>
    public static DBModelMetadata Get
        <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] t_Type>
        () where t_Type : IDBModel
    {
        if (s_Cache.TryGetValue(typeof(t_Type), out var l_Existing))
            return l_Existing;

        return s_Cache[typeof(t_Type)] = new DBModelMetadata(typeof(t_Type));
    }
    /// <summary>
    /// Get tables from cache of DBModelMetadata for a given DBInstance name
    /// </summary>f
    /// <param name="dbInstanceName">DBInstance name</param>
    /// <returns>List of tables using the DBInstance name</returns>
    public static IEnumerable<DBModelMetadata> GetTablesByInstanceName(string dbInstanceName)
    {
        return s_Cache.Values.Where(x => x.TableAttribute.DBInstanceName == dbInstanceName);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Init all the DBModels
    /// </summary>
    [RequiresUnreferencedCode("Calls System.Reflection.Assembly.GetTypes()")]
    internal static void InitAll()
    {
        var l_Assemblies = Array.Empty<Assembly>();
        try { l_Assemblies = AppDomain.CurrentDomain.GetAssemblies(); } catch (Exception) { }

        foreach (var l_Assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var l_Types = Array.Empty<Type>();
            try { l_Types = l_Assembly.GetTypes(); } catch (Exception) { }

            foreach (var l_Type in l_Types)
            {
                if (!l_Type.IsClass
                    || l_Type.ContainsGenericParameters
                    || l_Type.IsAbstract
                    || !typeof(IDBModel).IsAssignableFrom(l_Type)
                    || s_Cache.ContainsKey(l_Type))
                    continue;

                s_Cache[l_Type] = new DBModelMetadata(l_Type);
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly Type                            ModelType;
    public readonly string                          Identifier;
    public          Attributes.DBTableAttribute     TableAttribute      = null!;
    public          Attributes.DBFieldAttribute[]   FieldAttributes     = Array.Empty<Attributes.DBFieldAttribute>();
    public          Attributes.DBFieldAttribute?    AutoIncrementField;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="modelType">Type of the DBModel</param>
    /// <exception cref="Exception">If the DBModel does not have a DBTable attribute</exception>
    public DBModelMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type modelType
        )
    {
        var l_TableAttribute = modelType.GetCustomAttribute<Attributes.DBTableAttribute>();
        if (l_TableAttribute == null)
            throw new Exception($"DBModel {modelType.FullName} does not have a DBTable attribute");

        l_TableAttribute.Init(this, modelType);

        ModelType       = modelType;
        Identifier      = modelType.FullName!;
        TableAttribute  = l_TableAttribute;

        Console.WriteLine($"init of {l_TableAttribute.TableName}");

        var l_Fields        = modelType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        var l_ValidFields   = new List<Attributes.DBFieldAttribute>();
        foreach (FieldInfo l_Field in l_Fields)
        {
            if (!l_Field.IsDefined(typeof(Attributes.DBFieldAttribute), true))
                continue;

            var l_Attribute = l_Field.GetCustomAttribute<Attributes.DBFieldAttribute>()!;
            l_Attribute.Init(this, l_Field);

            l_ValidFields.Add(l_Attribute);
        }

        FieldAttributes = l_ValidFields.ToArray();
    }
}
