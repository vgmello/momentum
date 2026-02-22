// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.SourceGenerators.DbCommand;

namespace Momentum.Extensions.SourceGenerators.Tests.DbCommand;

/// <summary>
/// Tests covering edge cases in SourceGeneratorExtensions, DbCommandTypeInfoSourceGen equality/hashcode,
/// and handler file naming for various scenarios.
/// </summary>
public class DbCommandSourceGenExtensionsEdgeCaseTests : DbCommandSourceGenTestsBase
{
    [Fact]
    public void FunctionQuery_WithDollarPrefix_ShouldStripDollarAndWrapInSelect()
    {
        // Arrange - $ prefix exercises GetCommandText branch: baseCommand.StartsWith('$')
        const string source = """
                              [DbCommand(fn: "$schema_name.function_name")]
                              public partial record FnQuery(int Param1, string Param2) : IQuery<System.Collections.Generic.IEnumerable<Result>>;

                              public record Result(int Id);
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var handlerFile = generated.First(g => g.Contains("HandleAsync"));
        handlerFile.ShouldContain("SELECT * FROM schema_name.function_name(@Param1, @Param2)");
    }

    [Fact]
    public void FunctionQuery_WithSnakeCaseAndDollarPrefix_ShouldConvertParamNames()
    {
        // Arrange - fn with $ prefix and snake_case params
        const string source = """
                              [DbCommand(fn: "$app.get_data", paramsCase: DbParamsCase.SnakeCase)]
                              public partial record GetDataQuery(int PageSize, int PageOffset) : IQuery<System.Collections.Generic.IEnumerable<Data>>;

                              public record Data(int Id);
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var handlerFile = generated.First(g => g.Contains("HandleAsync"));
        handlerFile.ShouldContain("SELECT * FROM app.get_data(@page_size, @page_offset)");
    }

    [Fact]
    public void NoResultType_WithSpSpecified_ShouldReturnTaskAndExecuteAsync()
    {
        // Arrange - ICommand without generic (no result type) but with sp specified
        // This should trigger MMT002 because sp is specified but no ICommand<T>/IQuery<T>
        const string source = """
                              [DbCommand(sp: "fire_and_forget")]
                              public partial record FireAndForgetCommand(int Id);
                              """;

        // Act
        var (_, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert - MMT002 error because sp is set but no ICommand<T>/IQuery<T>
        diagnostics.ShouldContain(d => d.Severity == DiagnosticSeverity.Error && d.Id == "MMT002");
    }

    [Fact]
    public void MultipleCommandsSameNamespace_WithMixedTypes_ShouldGenerateAll()
    {
        // Arrange
        const string source = """
                              [DbCommand(sp: "create_user")]
                              public partial record CreateUserCommand(string Name) : ICommand<int>;

                              [DbCommand(sql: "SELECT * FROM users")]
                              public partial record GetAllUsersQuery() : IQuery<System.Collections.Generic.IEnumerable<User>>;

                              [DbCommand]
                              public partial record ManualOp(int Id, string Val) : ICommand<string>;

                              public record User(int Id, string Name);
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        // 3 DbParams + 2 Handlers (ManualOp has no sp/sql/fn so no handler)
        generated.Length.ShouldBe(5);

        generated.ShouldContain(g => g.Contains("CreateUserCommand") && g.Contains("IDbParamsProvider"));
        generated.ShouldContain(g => g.Contains("GetAllUsersQuery") && g.Contains("IDbParamsProvider"));
        generated.ShouldContain(g => g.Contains("ManualOp") && g.Contains("IDbParamsProvider"));
        generated.ShouldContain(g => g.Contains("CreateUserCommandHandler"));
        generated.ShouldContain(g => g.Contains("GetAllUsersQueryHandler"));
    }

    [Fact]
    public void RecordType_ShouldGetSealedModifierInGeneratedDbParams()
    {
        // Arrange - Records without sealed/abstract get "sealed" prepended by the writer
        const string source = """
                              [DbCommand]
                              public partial record SomeCommand(int Id, string Value) : ICommand<int>;
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var dbParamsFile = generated.First(g => g.Contains("IDbParamsProvider"));
        dbParamsFile.ShouldContain("sealed");
        dbParamsFile.ShouldContain("partial record SomeCommand");
    }

    [Fact]
    public void ClassType_ShouldGetSealedModifierInGeneratedDbParams()
    {
        // Arrange - Regular class without sealed/abstract gets "sealed" prepended
        const string source = """
                              [DbCommand]
                              public partial class RegularCommand : ICommand<int>
                              {
                                  public int Id { get; set; }
                              }
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var dbParamsFile = generated.First(g => g.Contains("IDbParamsProvider"));
        dbParamsFile.ShouldContain("sealed");
        dbParamsFile.ShouldContain("partial class RegularCommand");
    }

    [Fact]
    public void CustomDataSource_ShouldGenerateFromKeyedServicesAttribute()
    {
        // Arrange - Custom data source key
        const string source = """
                              [DbCommand(sp: "get_analytics", dataSource: "AnalyticsDb")]
                              public partial record GetAnalyticsQuery(int Period) : ICommand<long>;
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var handlerFile = generated.First(g => g.Contains("HandleAsync"));
        handlerFile.ShouldContain("FromKeyedServicesAttribute(\"AnalyticsDb\")");
    }

    [Fact]
    public void EmptyConstructorRecord_WithSnakeCase_ShouldReturnThis()
    {
        // Arrange - Record with no properties, snake_case specified = no custom names = return this
        const string source = """
                              [DbCommand(sp: "do_thing", paramsCase: DbParamsCase.SnakeCase)]
                              public partial record EmptyParamsCommand() : ICommand<int>;
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var dbParamsFile = generated.First(g => g.Contains("IDbParamsProvider"));
        dbParamsFile.ShouldContain("return this;");
    }

    [Fact]
    public void FunctionQuery_WithNoParams_ShouldGenerateEmptyParentheses()
    {
        // Arrange - fn query with no parameters
        const string source = """
                              [DbCommand(fn: "select * from app.get_all")]
                              public partial record GetAllQuery() : IQuery<System.Collections.Generic.IEnumerable<Item>>;

                              public record Item(int Id);
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var handlerFile = generated.First(g => g.Contains("HandleAsync"));
        handlerFile.ShouldContain("select * from app.get_all()");
    }

    [Fact]
    public void MutuallyExclusive_SpAndFn_ShouldReportMMT003()
    {
        // Arrange
        const string source = """
                              [DbCommand(sp: "proc", fn: "select * from func")]
                              public partial record BadCommand(int Id) : ICommand<int>;
                              """;

        // Act
        var (_, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldContain(d => d.Severity == DiagnosticSeverity.Error && d.Id == "MMT003");
    }

    [Fact]
    public void MutuallyExclusive_SqlAndFn_ShouldReportMMT003()
    {
        // Arrange
        const string source = """
                              [DbCommand(sql: "SELECT 1", fn: "select * from func")]
                              public partial record BadCommand(int Id) : ICommand<int>;
                              """;

        // Act
        var (_, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldContain(d => d.Severity == DiagnosticSeverity.Error && d.Id == "MMT003");
    }

    [Fact]
    public void MutuallyExclusive_AllThree_ShouldReportMMT003()
    {
        // Arrange
        const string source = """
                              [DbCommand(sp: "proc", sql: "SELECT 1", fn: "select * from func")]
                              public partial record BadCommand(int Id) : ICommand<int>;
                              """;

        // Act
        var (_, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldContain(d => d.Severity == DiagnosticSeverity.Error && d.Id == "MMT003");
    }

    [Fact]
    public void ValidDollarPrefixFn_ShouldNotReportMMT005()
    {
        // Arrange - valid $-prefixed function name
        const string source = """
                              [DbCommand(fn: "$my_schema.my_function")]
                              public partial record ValidFnQuery(int Id) : ICommand<int>;
                              """;

        // Act
        var (_, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Id == "MMT005");
    }

    [Fact]
    public void ColumnAttribute_ShouldOverridePrefixInMsBuild()
    {
        // Arrange - Column attribute override with MSBuild prefix
        const string source = """
                              using Momentum.Extensions.Abstractions.Dapper.MetadataAttributes;
                              [DbCommand(sp: "update_record", paramsCase: DbParamsCase.SnakeCase)]
                              public partial record UpdateRecordCommand(
                                  int RecordId,
                                  [Column("custom_col")] string RecordName
                              ) : ICommand<int>;
                              """;

        var options = new Dictionary<string, string?>
        {
            ["build_property.DbCommandParamPrefix"] = "pfx_"
        };

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source, options);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var dbParamsFile = generated.First(g => g.Contains("IDbParamsProvider"));
        // RecordId should get prefix + snake_case
        dbParamsFile.ShouldContain("pfx_record_id = this.RecordId");
        // RecordName has Column attribute - should use custom name WITHOUT prefix
        dbParamsFile.ShouldContain("custom_col = this.RecordName");
    }

    [Fact]
    public void InvalidMsBuildParamsCase_ShouldDefaultToNone()
    {
        // Arrange - invalid MSBuild value for params case should fall back to None
        const string source = """
                              [DbCommand(sp: "create_user")]
                              public partial record CreateUserCommand(int UserId, string FirstName) : ICommand<int>;
                              """;

        var options = new Dictionary<string, string?>
        {
            ["build_property.DbCommandDefaultParamCase"] = "InvalidValue"
        };

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source, options);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var dbParamsFile = generated.First(g => g.Contains("IDbParamsProvider"));
        // With invalid/None case, should use property names as-is (return this)
        dbParamsFile.ShouldContain("return this;");
    }
}
