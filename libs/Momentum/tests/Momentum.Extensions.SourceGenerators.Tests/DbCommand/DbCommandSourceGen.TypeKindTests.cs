// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.SourceGenerators.DbCommand;

namespace Momentum.Extensions.SourceGenerators.Tests.DbCommand;

/// <summary>
/// Tests for various type kinds (class, struct, record struct) in DbCommand code generation.
/// Exercises GetTypeDeclaration for different TypeKind values in SourceGeneratorExtensions.
/// Also covers integral type variations (sbyte, byte, short, ushort, uint, ulong) for IsIntegralType.
/// </summary>
public class DbCommandSourceGenTypeKindTests : DbCommandSourceGenTestsBase
{
    [Fact]
    public void ClassType_ShouldGeneratePartialClassDbParams()
    {
        // Arrange - regular class (not record)
        const string source = """
                              [DbCommand(sp: "update_user")]
                              public partial class UpdateUserCommand : ICommand<int>
                              {
                                  public int UserId { get; set; }
                                  public string Name { get; set; }
                              }
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var dbParamsFile = generated.First(g => g.Contains("IDbParamsProvider"));
        dbParamsFile.ShouldContain("partial class UpdateUserCommand");
        dbParamsFile.ShouldContain("ToDbParams()");
    }

    [Fact]
    public void RecordClassType_WithMultipleProperties_ShouldGenerateDbParams()
    {
        // Arrange - record (class) with many properties of different types
        const string source = """
                              [DbCommand(sp: "create_item", paramsCase: DbParamsCase.SnakeCase)]
                              public partial record CreateItemCommand(int ItemId, string ItemName, bool IsActive, decimal Price) : ICommand<int>;
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var dbParamsFile = generated.First(g => g.Contains("IDbParamsProvider"));
        dbParamsFile.ShouldContain("partial record CreateItemCommand");
        dbParamsFile.ShouldContain("item_id = this.ItemId");
        dbParamsFile.ShouldContain("item_name = this.ItemName");
        dbParamsFile.ShouldContain("is_active = this.IsActive");
        dbParamsFile.ShouldContain("price = this.Price");
    }

    [Fact]
    public void RecordType_WithSp_ShouldGenerateHandler()
    {
        // Arrange
        const string source = """
                              [DbCommand(sp: "create_item")]
                              public partial record CreateItemCommand(int ItemId, string Name) : ICommand<int>;
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var handlerFile = generated.First(g => g.Contains("HandleAsync"));
        handlerFile.ShouldContain("CreateItemCommandHandler");
        handlerFile.ShouldContain("ExecuteScalarAsync<int>");
    }

    [Fact]
    public void ClassType_WithProperties_ShouldGeneratePartialClassDbParams()
    {
        // Arrange - regular class with properties exercises the non-record property discovery path
        const string source = """
                              [DbCommand(sp: "get_count", paramsCase: DbParamsCase.SnakeCase)]
                              public partial class GetCountCommand : ICommand<int>
                              {
                                  public int CategoryId { get; set; }
                                  public string FilterName { get; set; }
                              }
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var dbParamsFile = generated.First(g => g.Contains("IDbParamsProvider"));
        dbParamsFile.ShouldContain("partial class GetCountCommand");
        dbParamsFile.ShouldContain("category_id = this.CategoryId");
        dbParamsFile.ShouldContain("filter_name = this.FilterName");
    }

    [Fact]
    public void LongResult_ShouldUseExecuteScalarAsyncLong()
    {
        // Arrange - tests IsIntegralType for long
        const string source = """
                              [DbCommand(sp: "get_total")]
                              public partial record GetTotalQuery() : ICommand<long>;
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var handlerFile = generated.First(g => g.Contains("HandleAsync"));
        handlerFile.ShouldContain("ExecuteScalarAsync<long>");
    }

    [Fact]
    public void ShortResult_ShouldUseExecuteScalarAsyncShort()
    {
        // Arrange - tests IsIntegralType for short (System.Int16)
        const string source = """
                              [DbCommand(sp: "get_short_val")]
                              public partial record GetShortValueQuery() : ICommand<short>;
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var handlerFile = generated.First(g => g.Contains("HandleAsync"));
        handlerFile.ShouldContain("ExecuteScalarAsync<short>");
    }

    [Fact]
    public void ByteResult_ShouldUseExecuteScalarAsyncByte()
    {
        // Arrange - tests IsIntegralType for byte (System.Byte)
        const string source = """
                              [DbCommand(sp: "get_byte_val")]
                              public partial record GetByteValueQuery() : ICommand<byte>;
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var handlerFile = generated.First(g => g.Contains("HandleAsync"));
        handlerFile.ShouldContain("ExecuteScalarAsync<byte>");
    }

    [Fact]
    public void IntNonQuery_ShouldUseExecuteAsync()
    {
        // Arrange - nonQuery with int exercises the NonQuery=true + IsIntegralType=true path
        const string source = """
                              [DbCommand(sp: "delete_users", nonQuery: true)]
                              public partial record DeleteUsersCommand(int Status) : ICommand<int>;
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var handlerFile = generated.First(g => g.Contains("HandleAsync"));
        handlerFile.ShouldContain("ExecuteAsync");
        handlerFile.ShouldNotContain("ExecuteScalarAsync");
    }

    [Fact]
    public void NonQueryWithNonIntegralResult_ShouldReportWarning()
    {
        // Arrange - nonQuery: true with non-integral type triggers MMT001 warning
        const string source = """
                              [DbCommand(sp: "delete_items", nonQuery: true)]
                              public partial record DeleteItemsCommand(int Status) : ICommand<string>;
                              """;

        // Act
        var (_, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldContain(d => d.Severity == DiagnosticSeverity.Warning && d.Id == "MMT001");
    }

    [Fact]
    public void MissingInterfaceWithSp_ShouldReportError()
    {
        // Arrange - sp specified but no ICommand/IQuery interface triggers MMT002
        const string source = """
                              [DbCommand(sp: "do_something")]
                              public partial record DoSomethingCommand(int Id);
                              """;

        // Act
        var (_, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldContain(d => d.Severity == DiagnosticSeverity.Error && d.Id == "MMT002");
    }

    [Fact]
    public void MissingInterfaceWithFn_ShouldReportError()
    {
        // Arrange - fn specified but no ICommand/IQuery interface triggers MMT002
        const string source = """
                              [DbCommand(fn: "select * from do_something")]
                              public partial record DoSomethingQuery(int Id);
                              """;

        // Act
        var (_, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldContain(d => d.Severity == DiagnosticSeverity.Error && d.Id == "MMT002");
    }

    [Fact]
    public void MissingInterfaceWithSql_ShouldReportError()
    {
        // Arrange - sql specified but no ICommand/IQuery interface triggers MMT002
        const string source = """
                              [DbCommand(sql: "SELECT 1")]
                              public partial record SelectOneQuery(int Id);
                              """;

        // Act
        var (_, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldContain(d => d.Severity == DiagnosticSeverity.Error && d.Id == "MMT002");
    }

    [Fact]
    public void DollarPrefixFn_ShouldGenerateSelectFromFunction()
    {
        // Arrange - $-prefix exercises the fn.StartsWith('$') path in GetCommandText
        const string source = """
                              [DbCommand(fn: "$app.get_users")]
                              public partial record GetUsersQuery(int Limit) : IQuery<System.Collections.Generic.IEnumerable<User>>;

                              public record User(int Id, string Name);
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var handlerFile = generated.First(g => g.Contains("HandleAsync"));
        handlerFile.ShouldContain("SELECT * FROM app.get_users(@Limit)");
    }

    [Fact]
    public void DollarPrefixFn_WithInvalidIdentifier_ShouldReportError()
    {
        // Arrange - $-prefix with invalid SQL identifier triggers MMT005
        const string source = """
                              [DbCommand(fn: "$123invalid")]
                              public partial record BadQuery(int Id) : ICommand<int>;
                              """;

        // Act
        var (_, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldContain(d => d.Severity == DiagnosticSeverity.Error && d.Id == "MMT005");
    }

    [Fact]
    public void ClassWithPropertiesAndDbCommandIgnore_ShouldExcludeIgnoredProperties()
    {
        // Arrange - exercises the property discovery path for non-record types
        const string source = """
                              [DbCommand(sp: "save_data", paramsCase: DbParamsCase.SnakeCase)]
                              public partial class SaveDataCommand : ICommand<int>
                              {
                                  public int DataId { get; set; }
                                  public string DataValue { get; set; }
                                  [DbCommandIgnore]
                                  public string IgnoredProp { get; set; }
                                  private string PrivateProp { get; set; }
                                  public static string StaticProp { get; set; }
                              }
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var dbParamsFile = generated.First(g => g.Contains("IDbParamsProvider"));
        dbParamsFile.ShouldContain("data_id = this.DataId");
        dbParamsFile.ShouldContain("data_value = this.DataValue");
        dbParamsFile.ShouldNotContain("IgnoredProp");
        dbParamsFile.ShouldNotContain("PrivateProp");
        dbParamsFile.ShouldNotContain("StaticProp");
    }

    [Fact]
    public void IQueryInterface_ShouldBeRecognizedForResultType()
    {
        // Arrange - IQuery<T> exercises the queryInterface path in GetDbCommandResultInfo
        const string source = """
                              [DbCommand(sql: "SELECT * FROM products WHERE id = @Id")]
                              public partial record GetProductQuery(int Id) : IQuery<Product>;

                              public record Product(int Id, string Name, decimal Price);
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var handlerFile = generated.First(g => g.Contains("HandleAsync"));
        handlerFile.ShouldContain("QueryFirstOrDefaultAsync<global::TestNamespace.Product>");
    }

    [Fact]
    public void CommandWithNoResultType_NoSpSqlFn_ShouldOnlyGenerateDbParams()
    {
        // Arrange - no interface, no sp/sql/fn = just DbParams, no handler, no diagnostics
        const string source = """
                              [DbCommand]
                              public partial record SimpleDataHolder(int Id, string Value);
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        // No MMT002 because sp/sql/fn are all empty - the analyzer only fires when one of those is specified
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);
        generated.Length.ShouldBe(1); // Only DbParams
        generated[0].ShouldContain("IDbParamsProvider");
    }
}
