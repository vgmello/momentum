// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.SourceGenerators.DbCommand;

namespace Momentum.Extensions.SourceGenerators.Tests.DbCommand;

public class DbCommandSourceGenHandlerTests : DbCommandSourceGenTestsBase
{
    public static IEnumerable<TheoryDataRow<string, string>> HandlerTestData =>
    [
        TestCase(
            name: "SP NonQuery with integer result",
            source:
            """
            [DbCommand(sp: "create_user", nonQuery: true)]
            public partial record CreateUserCommand(int UserId, string Name) : ICommand<int>;
            """,
            expected:
            """
            public static class CreateUserCommandHandler
            {
                public static async global::System.Threading.Tasks.Task<int> HandleAsync(global::TestNamespace.CreateUserCommand command, global::System.Data.Common.DbDataSource datasource, global::System.Threading.CancellationToken cancellationToken = default)
                {
                    await using var connection = await datasource.OpenConnectionAsync(cancellationToken);
                    var dbParams = command.ToDbParams();
                    return await global::Dapper.SqlMapper.ExecuteAsync(connection, new global::Dapper.CommandDefinition("create_user", dbParams, commandType: global::System.Data.CommandType.StoredProcedure, cancellationToken: cancellationToken));
                }
            }
            """
        ),
        TestCase(
            name: "SP with integer scalar result",
            source:
            """
            [DbCommand(sp: "create_user")]
            public partial record CreateUserCommand(int UserId, string Name) : ICommand<int>;
            """,
            expected:
            """
            public static class CreateUserCommandHandler
            {
                public static async global::System.Threading.Tasks.Task<int> HandleAsync(global::TestNamespace.CreateUserCommand command, global::System.Data.Common.DbDataSource datasource, global::System.Threading.CancellationToken cancellationToken = default)
                {
                    await using var connection = await datasource.OpenConnectionAsync(cancellationToken);
                    var dbParams = command.ToDbParams();
                    return await global::Dapper.SqlMapper.ExecuteScalarAsync<int>(connection, new global::Dapper.CommandDefinition("create_user", dbParams, commandType: global::System.Data.CommandType.StoredProcedure, cancellationToken: cancellationToken));
                }
            }
            """
        ),
        TestCase(
            name: "SQL query with IEnumerable result",
            source:
            """
            [DbCommand(sql: "SELECT * FROM users WHERE active = @Active")]
            public partial record GetActiveUsersQuery(bool Active) : ICommand<System.Collections.Generic.IEnumerable<User>>;

            public record User(int Id, string Name);
            """,
            expected:
            """
            public static class GetActiveUsersQueryHandler
            {
                public static async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<global::TestNamespace.User>> HandleAsync(global::TestNamespace.GetActiveUsersQuery command, global::System.Data.Common.DbDataSource datasource, global::System.Threading.CancellationToken cancellationToken = default)
                {
                    await using var connection = await datasource.OpenConnectionAsync(cancellationToken);
                    var dbParams = command.ToDbParams();
                    return await global::Dapper.SqlMapper.QueryAsync<global::TestNamespace.User>(connection, new global::Dapper.CommandDefinition("SELECT * FROM users WHERE active = @Active", dbParams, commandType: global::System.Data.CommandType.Text, cancellationToken: cancellationToken));
                }
            }
            """
        ),
        TestCase(
            name: "SQL query with single object result",
            source:
            """
            [DbCommand(sql: "SELECT * FROM users WHERE id = @UserId")]
            public partial record GetUserByIdQuery(int UserId) : ICommand<User>;

            public record User(int Id, string Name);
            """,
            expected:
            """
            public static class GetUserByIdQueryHandler
            {
                public static async global::System.Threading.Tasks.Task<global::TestNamespace.User> HandleAsync(global::TestNamespace.GetUserByIdQuery command, global::System.Data.Common.DbDataSource datasource, global::System.Threading.CancellationToken cancellationToken = default)
                {
                    await using var connection = await datasource.OpenConnectionAsync(cancellationToken);
                    var dbParams = command.ToDbParams();
                    return await global::Dapper.SqlMapper.QueryFirstOrDefaultAsync<global::TestNamespace.User>(connection, new global::Dapper.CommandDefinition("SELECT * FROM users WHERE id = @UserId", dbParams, commandType: global::System.Data.CommandType.Text, cancellationToken: cancellationToken));
                }
            }
            """
        ),
        TestCase(
            name: "SQL query with scalar integer result",
            source:
            """
            [DbCommand(sql: "SELECT COUNT(*) FROM users")]
            public partial record GetUserCountQuery() : ICommand<int>;
            """,
            expected:
            """
            public static class GetUserCountQueryHandler
            {
                public static async global::System.Threading.Tasks.Task<int> HandleAsync(global::TestNamespace.GetUserCountQuery command, global::System.Data.Common.DbDataSource datasource, global::System.Threading.CancellationToken cancellationToken = default)
                {
                    await using var connection = await datasource.OpenConnectionAsync(cancellationToken);
                    var dbParams = command.ToDbParams();
                    return await global::Dapper.SqlMapper.ExecuteScalarAsync<int>(connection, new global::Dapper.CommandDefinition("SELECT COUNT(*) FROM users", dbParams, commandType: global::System.Data.CommandType.Text, cancellationToken: cancellationToken));
                }
            }
            """
        ),
        TestCase(
            name: "SP with custom keyed data source",
            source:
            """
            [DbCommand(sp: "get_report", dataSource: "ReportingDb")]
            public partial record GetReportQuery(int ReportId) : ICommand<Report>;

            public record Report(int Id, string Title);
            """,
            expected:
            """
            public static class GetReportQueryHandler
            {
                public static async global::System.Threading.Tasks.Task<global::TestNamespace.Report> HandleAsync(global::TestNamespace.GetReportQuery command, [global::Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute("ReportingDb")] global::System.Data.Common.DbDataSource datasource, global::System.Threading.CancellationToken cancellationToken = default)
                {
                    await using var connection = await datasource.OpenConnectionAsync(cancellationToken);
                    var dbParams = command.ToDbParams();
                    return await global::Dapper.SqlMapper.QueryFirstOrDefaultAsync<global::TestNamespace.Report>(connection, new global::Dapper.CommandDefinition("get_report", dbParams, commandType: global::System.Data.CommandType.StoredProcedure, cancellationToken: cancellationToken));
                }
            }
            """
        ),
        TestCase(
            name: "SQL query with long scalar result",
            source:
            """
            [DbCommand(sql: "SELECT @@IDENTITY")]
            public partial record GetLastInsertIdQuery() : ICommand<long>;
            """,
            expected:
            """
            public static class GetLastInsertIdQueryHandler
            {
                public static async global::System.Threading.Tasks.Task<long> HandleAsync(global::TestNamespace.GetLastInsertIdQuery command, global::System.Data.Common.DbDataSource datasource, global::System.Threading.CancellationToken cancellationToken = default)
                {
                    await using var connection = await datasource.OpenConnectionAsync(cancellationToken);
                    var dbParams = command.ToDbParams();
                    return await global::Dapper.SqlMapper.ExecuteScalarAsync<long>(connection, new global::Dapper.CommandDefinition("SELECT @@IDENTITY", dbParams, commandType: global::System.Data.CommandType.Text, cancellationToken: cancellationToken));
                }
            }
            """
        ),
        TestCase(
            name: "Function query with auto-generated parameters",
            source:
            """
            [DbCommand(fn: "select * from app_domain.invoices_get")]
            public partial record GetInvoicesDbQuery(int Limit, int Offset, string Status) : IQuery<System.Collections.Generic.IEnumerable<Invoice>>;

            public record Invoice(int Id, string Status);
            """,
            expected:
            """
            public static class GetInvoicesDbQueryHandler
            {
                public static async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<global::TestNamespace.Invoice>> HandleAsync(global::TestNamespace.GetInvoicesDbQuery command, global::System.Data.Common.DbDataSource datasource, global::System.Threading.CancellationToken cancellationToken = default)
                {
                    await using var connection = await datasource.OpenConnectionAsync(cancellationToken);
                    var dbParams = command.ToDbParams();
                    return await global::Dapper.SqlMapper.QueryAsync<global::TestNamespace.Invoice>(connection, new global::Dapper.CommandDefinition("select * from app_domain.invoices_get(@Limit, @Offset, @Status)", dbParams, commandType: global::System.Data.CommandType.Text, cancellationToken: cancellationToken));
                }
            }
            """
        ),
        TestCase(
            name: "Function query with snake_case parameters",
            source:
            """
            [DbCommand(fn: "select * from app_domain.invoices_get", paramsCase: DbParamsCase.SnakeCase)]
            public partial record GetInvoicesDbQuery(int Limit, int Offset, string Status) : IQuery<System.Collections.Generic.IEnumerable<Invoice>>;

            public record Invoice(int Id, string Status);
            """,
            expected:
            """
            public static class GetInvoicesDbQueryHandler
            {
                public static async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<global::TestNamespace.Invoice>> HandleAsync(global::TestNamespace.GetInvoicesDbQuery command, global::System.Data.Common.DbDataSource datasource, global::System.Threading.CancellationToken cancellationToken = default)
                {
                    await using var connection = await datasource.OpenConnectionAsync(cancellationToken);
                    var dbParams = command.ToDbParams();
                    return await global::Dapper.SqlMapper.QueryAsync<global::TestNamespace.Invoice>(connection, new global::Dapper.CommandDefinition("select * from app_domain.invoices_get(@limit, @offset, @status)", dbParams, commandType: global::System.Data.CommandType.Text, cancellationToken: cancellationToken));
                }
            }
            """
        )
    ];

    [Theory]
    [MemberData(nameof(HandlerTestData))]
    public void CommandScenarios_ShouldGenerateCorrectHandlers(string source, string expectedHandlerBody)
    {
        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var expectedCode = GeneratedCodeHeader + Environment.NewLine + expectedHandlerBody;
        GeneratedCodeShouldMatchExpected(generated[1], expectedCode);
    }


    [Fact]
    public void GenerateHandler_WhenBothSpAndSqlSpecified_ShouldHandleGracefully()
    {
        // Arrange
        const string source = $"""
                               [DbCommand(sp: "create_user", sql: "INSERT INTO users (name) VALUES (@Name)")]
                               public partial record CreateUserCommand(string Name) : ICommand<int>;
                               """;
        // Act
        var (_, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldContain(d => d.Severity == DiagnosticSeverity.Error && d.Id == "DB_COMMAND_GEN003");
        diagnostics.Length.ShouldBe(1);
    }

    [Fact]
    public void GenerateHandler_WhenMultipleCommandPropertiesSpecified_ShouldHandleGracefully()
    {
        // Arrange
        const string source = $"""
                               [DbCommand(sp: "create_user", fn: "select * from users_create")]
                               public partial record CreateUserCommand(string Name) : ICommand<int>;
                               """;
        // Act
        var (_, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldContain(d => d.Severity == DiagnosticSeverity.Error && d.Id == "DB_COMMAND_GEN003");
        diagnostics.Length.ShouldBe(1);
    }

    [Fact]
    public void GenerateHandler_WhenNoCommandPropertiesSpecified_ShouldNotGenerateHandler()
    {
        // Arrange
        const string source = """
                              [DbCommand]
                              public partial record ManualCommand(int Id, string Name) : ICommand<string>;
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        generated.Length.ShouldBe(1);
        generated.ShouldNotContain(g => g.Contains("Handler"));
    }

    private static TheoryDataRow<string, string> TestCase(string name, string source, string expected) =>
        new(source, expected) { TestDisplayName = name };
}
