// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.SourceGenerators.DbCommand;

namespace Momentum.Extensions.SourceGenerators.Tests.DbCommand;

public class DbCommandSourceGenDbParmsTests : DbCommandSourceGenTestsBase
{
    public static IEnumerable<TheoryDataRow<string, string>> CommandTestData =>
    [
        TestCase(
            name: "SP NonQuery with default param case",
            source:
            """
            [DbCommand(sp: "create_user", nonQuery: true)]
            public partial record CreateUserCommand(int UserId, string Name) : ICommand<int>;
            """,
            expected:
            """
            sealed public partial record CreateUserCommand : global::Momentum.Extensions.Abstractions.Dapper.IDbParamsProvider
            {
                public global::System.Object ToDbParams()
                {
                    return this;
                }
            }
            """
        ),
        TestCase(
            name: "SQL Text query with default param case",
            source:
            """
            [DbCommand(sql: "SELECT * FROM users WHERE active = @Active")]
            public partial record GetActiveUsersQuery(bool Active) : ICommand<System.Collections.Generic.IEnumerable<User>>;

            public record User(int Id, string Name);
            """,
            expected:
            """
            sealed public partial record GetActiveUsersQuery : global::Momentum.Extensions.Abstractions.Dapper.IDbParamsProvider
            {
                public global::System.Object ToDbParams()
                {
                    return this;
                }
            }
            """
        ),
        TestCase(
            name: "Manual Command (no SP/SQL) using default params",
            source:
            """
            [DbCommand]
            public partial record ManualCommand(int Id, string Name) : ICommand<string>;
            """,
            expected:
            """
            sealed public partial record ManualCommand : global::Momentum.Extensions.Abstractions.Dapper.IDbParamsProvider
            {
                public global::System.Object ToDbParams()
                {
                    return this;
                }
            }
            """
        ),
        TestCase(
            name: "Manual Command with snake_case parameter conversion",
            source:
            """
            [DbCommand(paramsCase: DbParamsCase.SnakeCase)]
            public partial record ManualCommand(int Id, string Name) : ICommand<string>;
            """,
            expected:
            """
            sealed public partial record ManualCommand : global::Momentum.Extensions.Abstractions.Dapper.IDbParamsProvider
            {
                public global::System.Object ToDbParams()
                {
                    var p = new {
                        id = this.Id,
                        name = this.Name
                    };
                    return p;
                }
            }
            """
        ),
        TestCase(
            name: "SP with snake_case params and multiple props",
            source:
            """
            [DbCommand(sp: "create_user", paramsCase: DbParamsCase.SnakeCase)]
            public partial record CreateUserCommand(int UserId, string FirstName, string LastName) : ICommand<int>;
            """,
            expected:
            """
            sealed public partial record CreateUserCommand : global::Momentum.Extensions.Abstractions.Dapper.IDbParamsProvider
            {
                public global::System.Object ToDbParams()
                {
                    var p = new
                    {
                        user_id = this.UserId,
                        first_name = this.FirstName,
                        last_name = this.LastName
                    };
                    return p;
                }
            }
            """
        ),
        TestCase(
            name: "SP with Column attribute custom naming",
            source:
            """
            using Momentum.Extensions.Abstractions.Dapper.MetadataAttributes;
            [DbCommand(sp: "update_user", paramsCase: DbParamsCase.SnakeCase)]
            public partial record UpdateUserCommand(
                int UserId,
                [Column("custom_name")] string FirstName,
                string LastName,
                [Column("email_address")] string EmailAddr
            ) : ICommand<int>;
            """,
            expected:
            """
            sealed public partial record UpdateUserCommand : global::Momentum.Extensions.Abstractions.Dapper.IDbParamsProvider
            {
                public global::System.Object ToDbParams()
                {
                    var p = new
                    {
                        user_id = this.UserId,
                        custom_name = this.FirstName,
                        last_name = this.LastName,
                        email_address = this.EmailAddr
                    };
                    return p;
                }
            }
            """
        ),
        TestCase(
            name: "SP with empty parameter list",
            source:
            """
            [DbCommand(sp: "simple_proc", paramsCase: DbParamsCase.SnakeCase)]
            public partial record SimpleCommand() : ICommand<int>;
            """,
            expected:
            """
            sealed public partial record SimpleCommand : global::Momentum.Extensions.Abstractions.Dapper.IDbParamsProvider
            {
                public global::System.Object ToDbParams()
                {
                    return this;
                }
            }
            """
        )
    ];

    [Theory]
    [MemberData(nameof(CommandTestData))]
    public void CommandScenarios_ShouldGenerateCorrectly(string source, string expectedSource)
    {
        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var expectedCode = GeneratedCodeHeader + Environment.NewLine + expectedSource;

        GeneratedCodeShouldMatchExpected(generated[0], expectedCode);
    }

    [Fact]
    public void GenerateDbParams_WhenMultipleCommandsInSameNamespace_ShouldGenerateMultipleFiles()
    {
        // Arrange
        const string source = """
                              [DbCommand]
                              public partial record CreateUserCommand(string Name) : ICommand<int>;

                              [DbCommand]
                              public partial record GetUserCountQuery() : ICommand<int>;

                              [DbCommand]
                              public partial record ManualCommand(int Id) : ICommand;
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        generated.Length.ShouldBe(3);
        generated.ShouldContain(e =>
                (
                    e.Contains("CreateUserCommand") ||
                    e.Contains("GetUserCountQuery") ||
                    e.Contains("ManualCommand")
                )
                && e.Contains("IDbParamsProvider") && e.Contains("ToDbParams()")
            , expectedCount: 3);

        foreach (var gSource in generated)
        {
            gSource.ShouldContain("IDbParamsProvider");
            gSource.ShouldContain("ToDbParams()");
        }
    }

    private static TheoryDataRow<string, string> TestCase(string name, string source, string expected, string? skip = null) =>
        new(source, expected) { TestDisplayName = name, Skip = skip };
}
