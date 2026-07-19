using SSC.Db.Attributes;

namespace SSC.Db.Tests;

public class DbModelMetadataTests
{
    [Fact]
    public void GetBuildsAndCachesMappedModelMetadata()
    {
        DbModelMetadata metadata = DbModelMetadata.Get<CoverageModel>();

        Assert.Same(metadata, DbModelMetadata.Get<CoverageModel>());
        Assert.Equal(typeof(CoverageModel), metadata.ModelType);
        Assert.Equal("coverage", metadata.TableAttribute.DbInstanceName);
        Assert.Equal("coverage", metadata.TableAttribute.TableName);
        Assert.Equal(2, metadata.FieldAttributes.Length);
        Assert.Single(metadata.PrimaryFieldAttributes);
        Assert.Same(metadata.PrimaryFieldAttributes[0], metadata.AutoIncrementField);
        Assert.Equal("display_name", metadata.FieldAttributes[1].FieldName);
    }

    [Fact]
    public void ConstructorRejectsTypeWithoutTableMapping()
    {
        Exception exception = Assert.Throws<Exception>(() => new DbModelMetadata(typeof(UnmappedModel)));

        Assert.Contains("does not have a DBTable attribute", exception.Message);
    }

    [Fact]
    public void ConstructorRejectsInvalidModelName()
    {
        Exception exception = Assert.Throws<Exception>(() => new DbModelMetadata(typeof(Invalid_NameModel)));

        Assert.Contains("'_' is not permitted", exception.Message);
    }

    [Fact]
    public void ConstructorRejectsMultipleAutoIncrementFields()
    {
        Exception exception = Assert.Throws<Exception>(() => new DbModelMetadata(typeof(MultipleIdentityModel)));

        Assert.Contains("multiple auto incremented field", exception.Message);
    }

    [Fact]
    public void ConstructorRejectsNonPrimitiveAutoIncrementField()
    {
        Exception exception = Assert.Throws<Exception>(() => new DbModelMetadata(typeof(InvalidIdentityModel)));

        Assert.Contains("need to be a primitive type", exception.Message);
    }

    [Fact]
    public void GetTablesByInstanceNameReturnsCachedMappingsOnly()
    {
        DbModelMetadata expected = DbModelMetadata.Get<CoverageModel>();

        Assert.Contains(expected, DbModelMetadata.GetTablesByInstanceName("coverage"));
        Assert.DoesNotContain(expected, DbModelMetadata.GetTablesByInstanceName("another-instance"));
    }

    [Fact]
    public void ToStringFormatsValuesAndRedactsSensitiveFields()
    {
        var model = new PrintableModel
        {
            Text = "quoted \"value\"\n",
            Character = 'x',
            Number = 42,
            Optional = null,
            Value = new Version(1, 2),
            Secret = "hidden"
        };

        Assert.Equal(
            "PrintableModel { Text = \"quoted \\\"value\\\"\\n\", Character = 'x', Number = 42, " +
            "Optional = null, Value = 1.2, Secret = <redacted> }",
            model.ToString());
    }

    [DbTable("coverage")]
    private sealed class CoverageModel : DbModel<CoverageModel>
    {
        [DbField(primaryKey: true, autoIncrement: true)]
        public int Id = 0;

        [DbField]
        public string DisplayName = string.Empty;
    }

    private sealed class UnmappedModel
    {
    }

    [DbTable("coverage")]
    private sealed class Invalid_NameModel
    {
    }

    [DbTable("coverage")]
    private sealed class MultipleIdentityModel
    {
        [DbField(autoIncrement: true)]
        public int First = 0;

        [DbField(autoIncrement: true)]
        public int Second = 0;
    }

    [DbTable("coverage")]
    private sealed class InvalidIdentityModel
    {
        [DbField(autoIncrement: true)]
        public decimal Id = 0;
    }

    [DbTable("coverage")]
    private sealed class PrintableModel : DbModel<PrintableModel>
    {
        [DbField]
        public string Text = string.Empty;

        [DbField]
        public char Character;

        [DbField]
        public int Number;

        [DbField]
        public int? Optional;

        [DbField]
        public object Value = new();

        [DbField(sensitive: true)]
        public string Secret = string.Empty;
    }
}
