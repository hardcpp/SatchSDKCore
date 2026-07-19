using SSC.Db.Attributes;
using SSC.Db.Querying;

namespace SSC.Db.Expressions;

/// <summary>
/// DB dialect interface
/// </summary>
public abstract class IDbQueryDialect
{
    /// <summary>
    /// Gets whether generated identities require a command separate from the insert.
    /// </summary>
    public abstract bool AutoIncrementResultIsSeparateQuery { get; }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Appends a projection of every mapped field and its source table.
    /// </summary>
    /// <param name="query">The destination query.</param>
    public abstract void SelectAllFrom(DbQuery query);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Appends the qualified mapped table name.
    /// </summary>
    /// <param name="query">The destination query.</param>
    public abstract void Table(DbQuery query);

    /// <summary>
    /// Appends a table-qualified mapped field name.
    /// </summary>
    /// <param name="query">The destination query.</param>
    /// <param name="field">The mapped field.</param>
    public abstract void Field(DbQuery query, DbFieldAttribute field);

    /// <summary>
    /// Appends an unqualified mapped field name.
    /// </summary>
    /// <param name="query">The destination query.</param>
    /// <param name="field">The mapped field.</param>
    public abstract void FieldName(DbQuery query, DbFieldAttribute field);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Appends the dialect-specific default-values insert fragment.
    /// </summary>
    /// <param name="query">The destination query.</param>
    public abstract void DefaultValues(DbQuery query);

    /// <summary>
    /// Appends SQL that returns the generated identity.
    /// </summary>
    /// <param name="query">The destination query.</param>
    /// <param name="field">The generated field.</param>
    public abstract void AutoIncrementResult(DbQuery query, DbFieldAttribute field);

    /// <summary>
    /// Appends a lowercase transformation for a mapped field.
    /// </summary>
    /// <param name="query">The destination query.</param>
    /// <param name="field">The mapped field.</param>
    public abstract void FieldValueLower(DbQuery query, DbFieldAttribute field);

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Appends the beginning of a string concatenation expression.
    /// </summary>
    /// <param name="query">The destination query.</param>
    public abstract void StringConcatStart(DbQuery query);

    /// <summary>
    /// Appends the separator between string concatenation operands.
    /// </summary>
    /// <param name="query">The destination query.</param>
    public abstract void StringConcatSeparator(DbQuery query);

    /// <summary>
    /// Appends the end of a string concatenation expression.
    /// </summary>
    /// <param name="query">The destination query.</param>
    public abstract void StringConcatEnd(DbQuery query);
}
