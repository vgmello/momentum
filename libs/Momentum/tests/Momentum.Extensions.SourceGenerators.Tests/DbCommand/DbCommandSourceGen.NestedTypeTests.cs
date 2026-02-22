// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.SourceGenerators.DbCommand;

namespace Momentum.Extensions.SourceGenerators.Tests.DbCommand;

/// <summary>
/// Tests for nested DbCommand types, exercising SourceGenBaseWriter.AppendContainingTypeStarts/Ends,
/// DbCommandHandlerSourceGenWriter nested type paths, and DbCommandTypeInfo.SourceGen containing type tree.
/// </summary>
public class DbCommandSourceGenNestedTypeTests : DbCommandSourceGenTestsBase
{
    private const string NestedSourceImports = """
                                               using Momentum.Extensions.Abstractions.Dapper;
                                               using Momentum.Extensions.Abstractions.Messaging;

                                               namespace TestNamespace;
                                               """;

    [Fact]
    public void NestedType_ShouldGenerateDbParamsWithContainingTypeDeclarations()
    {
        // Arrange - A DbCommand nested inside a parent class
        const string source = """
                              public partial class CustomerCommands
                              {
                                  [DbCommand(sp: "create_customer")]
                                  public partial record CreateCustomerCommand(int CustomerId, string Name) : ICommand<int>;
                              }
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(NestedSourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        // Should generate both DbParams and Handler files
        generated.Length.ShouldBe(2);

        // DbParams file should contain the containing type wrapper
        var dbParamsFile = generated.First(g => g.Contains("IDbParamsProvider"));
        dbParamsFile.ShouldContain("partial class CustomerCommands");
        dbParamsFile.ShouldContain("CreateCustomerCommand");
        dbParamsFile.ShouldContain("ToDbParams()");
    }

    [Fact]
    public void NestedType_ShouldGenerateHandlerWithContainingTypeDeclarations()
    {
        // Arrange - A DbCommand nested inside a parent class
        const string source = """
                              public partial class CustomerCommands
                              {
                                  [DbCommand(sp: "create_customer")]
                                  public partial record CreateCustomerCommand(int CustomerId, string Name) : ICommand<int>;
                              }
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(NestedSourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        // Handler file should have the containing type wrapping the handler method
        var handlerFile = generated.First(g => g.Contains("HandleAsync"));
        handlerFile.ShouldContain("partial class CustomerCommands");
        handlerFile.ShouldContain("HandleAsync");
        handlerFile.ShouldContain("GeneratedCodeAttribute");
        // The GeneratedCodeAttribute should be on the method, not on a separate handler class
        handlerFile.ShouldNotContain("public static class CreateCustomerCommandHandler");
    }

    [Fact]
    public void DeeplyNestedType_ShouldGenerateAllContainingTypeWrappers()
    {
        // Arrange - Two levels of nesting
        const string source = """
                              public partial class OuterContainer
                              {
                                  public partial class InnerContainer
                                  {
                                      [DbCommand(sp: "deep_query")]
                                      public partial record DeepQuery(int Id) : ICommand<int>;
                                  }
                              }
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(NestedSourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);
        generated.Length.ShouldBe(2);

        // Both generated files should contain both containing types
        var dbParamsFile = generated.First(g => g.Contains("IDbParamsProvider"));
        dbParamsFile.ShouldContain("partial class OuterContainer");
        dbParamsFile.ShouldContain("partial class InnerContainer");

        var handlerFile = generated.First(g => g.Contains("HandleAsync"));
        handlerFile.ShouldContain("partial class OuterContainer");
        handlerFile.ShouldContain("partial class InnerContainer");
    }

    [Fact]
    public void NestedType_WithSnakeCaseParams_ShouldGenerateCorrectDbParams()
    {
        // Arrange
        const string source = """
                              public partial class OrderCommands
                              {
                                  [DbCommand(sp: "create_order", paramsCase: DbParamsCase.SnakeCase)]
                                  public partial record CreateOrderCommand(int OrderId, string CustomerName) : ICommand<int>;
                              }
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(NestedSourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var dbParamsFile = generated.First(g => g.Contains("IDbParamsProvider"));
        dbParamsFile.ShouldContain("order_id = this.OrderId");
        dbParamsFile.ShouldContain("customer_name = this.CustomerName");
        dbParamsFile.ShouldContain("partial class OrderCommands");
    }

    [Fact]
    public void NestedType_WithSqlQuery_ShouldGenerateHandlerInsideContainingType()
    {
        // Arrange
        const string source = """
                              public partial class UserQueries
                              {
                                  [DbCommand(sql: "SELECT * FROM users WHERE id = @Id")]
                                  public partial record GetUserQuery(int Id) : ICommand<User>;
                              }

                              public record User(int Id, string Name);
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(NestedSourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var handlerFile = generated.First(g => g.Contains("HandleAsync"));
        handlerFile.ShouldContain("partial class UserQueries");
        handlerFile.ShouldContain("QueryFirstOrDefaultAsync<global::TestNamespace.User>");
    }

    [Fact]
    public void NestedType_WithFunctionQuery_ShouldGenerateHandlerWithAutoParams()
    {
        // Arrange
        const string source = """
                              public partial class ReportQueries
                              {
                                  [DbCommand(fn: "select * from app.get_reports")]
                                  public partial record GetReportsQuery(int Limit, int Offset) : IQuery<System.Collections.Generic.IEnumerable<Report>>;
                              }

                              public record Report(int Id, string Title);
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(NestedSourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var handlerFile = generated.First(g => g.Contains("HandleAsync"));
        handlerFile.ShouldContain("partial class ReportQueries");
        handlerFile.ShouldContain("QueryAsync<global::TestNamespace.Report>");
        handlerFile.ShouldContain("select * from app.get_reports(@Limit, @Offset)");
    }

    [Fact]
    public void NestedType_ManualCommand_ShouldGenerateOnlyDbParamsNoHandler()
    {
        // Arrange - Manual command (no sp/sql/fn) should only generate DbParams
        const string source = """
                              public partial class DataCommands
                              {
                                  [DbCommand]
                                  public partial record ManualDataCommand(int Id, string Value) : ICommand<string>;
                              }
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(NestedSourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);
        generated.Length.ShouldBe(1); // Only DbParams file
        generated[0].ShouldContain("IDbParamsProvider");
        generated[0].ShouldContain("partial class DataCommands");
        generated[0].ShouldNotContain("HandleAsync");
    }
}
