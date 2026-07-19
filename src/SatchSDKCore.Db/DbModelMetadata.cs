using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using SSC.Db.Attributes;

namespace SSC.Db;

/// <summary>
/// Describes the table and field mappings for a database model.
/// </summary>
public class DbModelMetadata
{
    private static readonly Dictionary<Type, DbModelMetadata> s_Cache = new();

    /// <summary>Gets all mapped fields in materialization order.</summary>
    public readonly DbFieldAttribute[] FieldAttributes;

    /// <summary>Gets the fully qualified model identifier.</summary>
    public readonly string Identifier;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>Gets the CLR model type.</summary>
    public readonly Type ModelType;

    /// <summary>Gets all fields participating in the primary key.</summary>
    public readonly DbFieldAttribute[] PrimaryFieldAttributes;

    /// <summary>Gets the model's table mapping.</summary>
    public readonly DbTableAttribute TableAttribute;

    /// <summary>Gets the auto-increment field, or <see langword="null" /> when absent.</summary>
    public DbFieldAttribute? AutoIncrementField;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="modelType">Type of the DbModel</param>
    /// <exception cref="Exception">If the DbModel does not have a DBTable attribute</exception>
    // ReSharper disable once MemberCanBePrivate.Global
    public DbModelMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        Type modelType
    )
    {
        var tableAttribute = modelType.GetCustomAttribute<DbTableAttribute>();
        if (tableAttribute == null)
        {
            throw new Exception($"DbModel {modelType.FullName} does not have a DBTable attribute");
        }

        tableAttribute.Init(this, modelType);

        ModelType = modelType;
        Identifier = modelType.FullName!;
        TableAttribute = tableAttribute;

        Console.WriteLine($"init of {tableAttribute.TableName}");

        FieldInfo[] fields =
            modelType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        var validFields = new List<DbFieldAttribute>();
        foreach (FieldInfo field in fields)
        {
            if (!field.IsDefined(typeof(DbFieldAttribute), true))
            {
                continue;
            }

            var dbFieldAttribute = field.GetCustomAttribute<DbFieldAttribute>()!;
            dbFieldAttribute.Init(this, field);

            validFields.Add(dbFieldAttribute);
        }

        FieldAttributes = validFields.ToArray();
        PrimaryFieldAttributes = FieldAttributes.Where(x => x.PrimaryKey).ToArray();
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Gets cached metadata for a model type, creating it when necessary.
    /// </summary>
    /// <typeparam name="TModel">Type of the DbModel</typeparam>
    /// <returns>DbModelMetadata</returns>
    public static DbModelMetadata Get
        <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TModel>
        () where TModel : IDbModel
    {
        if (s_Cache.TryGetValue(typeof(TModel), out DbModelMetadata? existing))
        {
            return existing;
        }

        return s_Cache[typeof(TModel)] = new DbModelMetadata(typeof(TModel));
    }

    /// <summary>
    /// Get tables from cache of DbModelMetadata for a given DbInstance name
    /// </summary>
    /// <param name="dbInstanceName">DbInstance name</param>
    /// <returns>List of tables using the DbInstance name</returns>
    public static IEnumerable<DbModelMetadata> GetTablesByInstanceName(string dbInstanceName) =>
        s_Cache.Values.Where(x => x.TableAttribute.DbInstanceName == dbInstanceName);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Initializes metadata for all loaded concrete database model types.
    /// </summary>
    [RequiresUnreferencedCode("Calls System.Reflection.Assembly.GetTypes()")]
    internal static void InitAll()
    {
        Assembly[] assemblies = Array.Empty<Assembly>();

        try
        {
            assemblies = AppDomain.CurrentDomain.GetAssemblies();
        }
        // ReSharper disable once EmptyGeneralCatchClause
        catch (Exception)
        {
        }

        foreach (Assembly assembly in assemblies)
        {
            Type[] types = Array.Empty<Type>();
            // ReSharper disable once EmptyGeneralCatchClause
            try
            {
                types = assembly.GetTypes();
            }
            catch (Exception)
            {
            }

            foreach (Type type in types)
            {
                if (!type.IsClass
                    || type.ContainsGenericParameters
                    || type.IsAbstract
                    || !typeof(IDbModel).IsAssignableFrom(type)
                    || s_Cache.ContainsKey(type))
                {
                    continue;
                }

                s_Cache[type] = new DbModelMetadata(type);
            }
        }
    }
}
