// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.SourceGenerators.DbCommand;

namespace Momentum.Extensions.SourceGenerators.Tests.DbCommand;

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
            sealed public partial record CreateUserCommand : global::Momentum.Extensions.Abstractions.Dapper.IDbParamsProvider
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
            sealed public partial record CreateUserCommand : global::Momentum.Extensions.Abstractions.Dapper.IDbParamsProvider
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
            sealed public partial record CreateUserCommand : global::Momentum.Extensions.Abstractions.Dapper.IDbParamsProvider
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

    public static IEnumerable<TheoryDataRow<string, string, string?, string?>> PrefixTestData =>
    [
        TestCase_WithPrefix(
            name: "Global prefix with SnakeCase",
            source:
            """
            [DbCommand(sp: "create_user")]
            public partial record CreateUserCommand(int UserId, string FirstName) : ICommand<int>;
            """,
            expected:
            """
            sealed public partial record CreateUserCommand : global::Momentum.Extensions.Abstractions.Dapper.IDbParamsProvider
            {
                public global::System.Object ToDbParams()
                {
                    var p = new
                    {
                        p_user_id = this.UserId,
                        p_first_name = this.FirstName
                    };
                    return p;
                }
            }
            """,
            msBuildParamsCase: "SnakeCase",
            msBuildPrefix: "p_"
        ),
        TestCase_WithPrefix(
            name: "Global prefix without case conversion",
            source:
            """
            [DbCommand(sp: "create_user")]
            public partial record CreateUserCommand(int UserId, string FirstName) : ICommand<int>;
            """,
            expected:
            """
            sealed public partial record CreateUserCommand : global::Momentum.Extensions.Abstractions.Dapper.IDbParamsProvider
            {
                public global::System.Object ToDbParams()
                {
                    var p = new
                    {
                        p_UserId = this.UserId,
                        p_FirstName = this.FirstName
                    };
                    return p;
                }
            }
            """,
            msBuildParamsCase: "None",
            msBuildPrefix: "p_"
        ),
        TestCase_WithPrefix(
            name: "Column attribute overrides prefix",
            source:
            """
            using Momentum.Extensions.Abstractions.Dapper.MetadataAttributes;
            [DbCommand(sp: "create_user", paramsCase: DbParamsCase.SnakeCase)]
            public partial record CreateUserCommand(int UserId, [Column("custom_name")] string FirstName) : ICommand<int>;
            """,
            expected:
            """
            sealed public partial record CreateUserCommand : global::Momentum.Extensions.Abstractions.Dapper.IDbParamsProvider
            {
                public global::System.Object ToDbParams()
                {
                    var p = new
                    {
                        p_user_id = this.UserId,
                        custom_name = this.FirstName
                    };
                    return p;
                }
            }
            """,
            msBuildParamsCase: null,
            msBuildPrefix: "p_"
        )
    ];

    [Theory]
    [MemberData(nameof(PrefixTestData))]
    public void PrefixScenarios_ShouldGenerateCorrectly(string source, string expectedSource, string? msBuildParamsCase, string? msBuildPrefix)
    {
        // Arrange
        var optionsProvider = new Dictionary<string, string?>
        {
            ["build_property.DbCommandDefaultParamCase"] = msBuildParamsCase,
            ["build_property.DbCommandParamPrefix"] = msBuildPrefix
        };

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source, optionsProvider);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var expectedCode = GeneratedCodeHeader + Environment.NewLine + expectedSource;

        GeneratedCodeShouldMatchExpected(generated[0], expectedCode);
    }

    private static TheoryDataRow<string, string, string?, string?> TestCase_WithPrefix(
        string name, string source, string expected, string? msBuildParamsCase, string? msBuildPrefix) =>
        new(source, expected, msBuildParamsCase, msBuildPrefix) { TestDisplayName = name };
}
