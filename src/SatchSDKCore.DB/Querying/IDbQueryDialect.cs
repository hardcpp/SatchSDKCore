using SSC.DB.Querying;

namespace SSC.DB.Expressions;

/// <summary>
/// DB dialect interface
/// </summary>
public abstract class IDbQueryDialect
{
    public abstract void SelectAllFrom(DbQueryBuilder queryBuilder);

    public abstract void Field(DbQueryBuilder queryBuilder, Attributes.DbFieldAttribute field);
    public abstract void FieldValueLower(DbQueryBuilder queryBuilder, Attributes.DbFieldAttribute field);
}
