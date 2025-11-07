using SSC.DB.Querying;

namespace SSC.DB.Expressions;

/// <summary>
/// DB dialect interface
/// </summary>
public abstract class IDBQueryDialect
{
    public abstract void SelectAllFrom(DBQueryBuilder queryBuilder);

    public abstract void Field(DBQueryBuilder queryBuilder, Attributes.DBFieldAttribute field);
    public abstract void FieldValueLower(DBQueryBuilder queryBuilder, Attributes.DBFieldAttribute field);
}
