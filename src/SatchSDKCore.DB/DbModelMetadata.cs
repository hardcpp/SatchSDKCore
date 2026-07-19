using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace SSC.DB;

/// <summary>
/// DB Model metadata
/// </summary>
public class DbModelMetadata
{
    private static readonly Dictionary<Type, DbModelMetadata> s_Cache = new();

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Get from cache of create DbModelMetadata for a type
    /// </summary>
    /// <typeparam name="TModel">Type of the DbModel</typeparam>
    /// <returns>DbModelMetadata</returns>
    public static DbModelMetadata Get
        <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TModel>
        () where TModel : IDbModel
    {
        if (s_Cache.TryGetValue(typeof(TModel), out var existing))
            return existing;

        return s_Cache[typeof(TModel)] = new DbModelMetadata(typeof(TModel));
    }
    /// <summary>
    /// Get tables from cache of DbModelMetadata for a given DbInstance name
    /// </summary>f
    /// <param name="dbInstanceName">DbInstance name</param>
    /// <returns>List of tables using the DbInstance name</returns>
    public static IEnumerable<DbModelMetadata> GetTablesByInstanceName(string dbInstanceName)
    {
        return s_Cache.Values.Where(x => x.TableAttribute.DbInstanceName == dbInstanceName);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Init all the DBModels
    /// </summary>
    [RequiresUnreferencedCode("Calls System.Reflection.Assembly.GetTypes()")]
    internal static void InitAll()
    {
        var assemblies = Array.Empty<Assembly>();

        try { assemblies = AppDomain.CurrentDomain.GetAssemblies(); }
        // ReSharper disable once EmptyGeneralCatchClause
        catch (Exception) { }

        foreach (var assembly in assemblies)
        {
            var types = Array.Empty<Type>();
            // ReSharper disable once EmptyGeneralCatchClause
            try { types = assembly.GetTypes(); } catch (Exception) { }

            foreach (var type in types)
            {
                if (!type.IsClass
                    || type.ContainsGenericParameters
                    || type.IsAbstract
                    || !typeof(IDbModel).IsAssignableFrom(type)
                    || s_Cache.ContainsKey(type))
                    continue;

                s_Cache[type] = new DbModelMetadata(type);
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public readonly Type                            ModelType;
    public readonly string                          Identifier;
    public readonly Attributes.DbTableAttribute     TableAttribute;
    public readonly Attributes.DbFieldAttribute[]   FieldAttributes;
    public          Attributes.DbFieldAttribute?    AutoIncrementField;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="modelType">Type of the DbModel</param>
    /// <exception cref="Exception">If the DbModel does not have a DBTable attribute</exception>
    // ReSharper disable once MemberCanBePrivate.Global
    public DbModelMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type modelType
        )
    {
        var tableAttribute = modelType.GetCustomAttribute<Attributes.DbTableAttribute>();
        if (tableAttribute == null)
            throw new Exception($"DbModel {modelType.FullName} does not have a DBTable attribute");

        tableAttribute.Init(this, modelType);

        ModelType       = modelType;
        Identifier      = modelType.FullName!;
        TableAttribute  = tableAttribute;

        Console.WriteLine($"init of {tableAttribute.TableName}");

        var fields = modelType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        var validFields = new List<Attributes.DbFieldAttribute>();
        foreach (FieldInfo field in fields)
        {
            if (!field.IsDefined(typeof(Attributes.DbFieldAttribute), true))
                continue;

            var dbFieldAttribute = field.GetCustomAttribute<Attributes.DbFieldAttribute>()!;
            dbFieldAttribute.Init(this, field);

            validFields.Add(dbFieldAttribute);
        }

        FieldAttributes = validFields.ToArray();
    }
}
