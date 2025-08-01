// Copyright (c) ABCDEG. All rights reserved.

using Operations.Extensions.SourceGenerators.DbCommand;

namespace Operations.Extensions.SourceGenerators.Tests.DbCommand;

public class DbCommandSourceGenMsBuildTests : DbCommandSourceGenTestsBase
{
    public static IEnumerable<TheoryDataRow<string, string, string?>> MsBuildTestData =>
    [
        TestCase(
            name: "Global SnakeCase property with unset paramsCase",
            source:
            """
            [DbCommand(sp: "create_user")]
            public partial record CreateUserCommand(int UserId, string FirstName, string LastName) : ICommand<int>;
            """,
            expected:
            """
            sealed public partial record CreateUserCommand : global::Operations.Extensions.Abstractions.Dapper.IDbParamsProvider
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
            """,
            msBuildProperty: "SnakeCase"
        ),
        TestCase(
            name: "Explicit paramsCase overrides MSBuild property",
            source:
            """
            [DbCommand(sp: "create_user", paramsCase: DbParamsCase.None)]
            public partial record CreateUserCommand(int UserId, string FirstName) : ICommand<int>;
            """,
            expected:
            """
            sealed public partial record CreateUserCommand : global::Operations.Extensions.Abstractions.Dapper.IDbParamsProvider
            {
                public global::System.Object ToDbParams()
                {
                    return this;
                }
            }
            """,
            msBuildProperty: "SnakeCase"
        ),
        TestCase(
            name: "Global None property with unset paramsCase",
            source:
            """
            [DbCommand(sp: "create_user")]
            public partial record CreateUserCommand(int UserId, string FirstName) : ICommand<int>;
            """,
            expected:
            """
            sealed public partial record CreateUserCommand : global::Operations.Extensions.Abstractions.Dapper.IDbParamsProvider
            {
                public global::System.Object ToDbParams()
                {
                    return this;
                }
            }
            """,
            msBuildProperty: "None"
        ),
        TestCase(
            name: "No MSBuild property with unset paramsCase",
            source:
            """
            [DbCommand(sp: "create_user")]
            public partial record CreateUserCommand(int UserId, string FirstName) : ICommand<int>;
            """,
            expected:
            """
            sealed public partial record CreateUserCommand : global::Operations.Extensions.Abstractions.Dapper.IDbParamsProvider
            {
                public global::System.Object ToDbParams()
                {
                    return this;
                }
            }
            """,
            msBuildProperty: null
        )
    ];

    [Theory]
    [MemberData(nameof(MsBuildTestData))]
    public void MSBuildScenarios_ShouldGenerateCorrectly(string source, string expectedSource, string? msBuildProperty)
    {
        // Arrange
        var optionsProvider = new Dictionary<string, string?>
        {
            ["build_property.DbCommandDefaultParamCase"] = msBuildProperty
        };

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source, optionsProvider);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var expectedCode = GeneratedCodeHeader + Environment.NewLine + expectedSource;

        GeneratedCodeShouldMatchExpected(generated[0], expectedCode);
    }

    private static TheoryDataRow<string, string, string?> TestCase(string name, string source, string expected, string? msBuildProperty) =>
        new(source, expected, msBuildProperty) { TestDisplayName = name };
}
