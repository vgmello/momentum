// Copyright (c) Momentum .NET. All rights reserved.

using Momentum.Extensions.Abstractions.Dapper;
using Momentum.Extensions.SourceGenerators.DbCommand;
using System.ComponentModel.DataAnnotations.Schema;

namespace Momentum.Extensions.SourceGenerators.Tests.DbCommand;

/// <summary>
/// Tests covering edge cases for the DbCommand source generator: empty/null sp/sql/fn skip paths
/// and DbCommandTypeInfoSourceGen equality behavior verified through incremental generator caching.
/// </summary>
public class DbCommandSourceGenEdgeCaseTests : DbCommandSourceGenTestsBase
{
    [Fact]
    public void DbCommand_WithNoSpSqlFn_ShouldOnlyGenerateDbParams()
    {
        // Arrange - [DbCommand] with no sp/sql/fn should skip handler generation entirely
        const string source = """
                              [DbCommand]
                              public partial record ManualCommand(int Id, string Name) : ICommand<string>;
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);
        generated.Length.ShouldBe(1);
        generated[0].ShouldContain("IDbParamsProvider");
        generated[0].ShouldContain("ToDbParams()");
        generated.ShouldNotContain(g => g.Contains("Handler"));
        generated.ShouldNotContain(g => g.Contains("HandleAsync"));
    }

    [Fact]
    public void DbCommand_WithEmptySpSqlFn_ShouldOnlyGenerateDbParams()
    {
        // Arrange - [DbCommand] with explicitly empty strings for sp/sql/fn
        // Empty strings are treated the same as null by the IsNullOrWhiteSpace check in GenerateHandlerPart
        const string source = """
                              [DbCommand(sp: "", sql: "", fn: "")]
                              public partial record EmptyStringsCommand(int Id, string Value) : ICommand<int>;
                              """;

        // Act
        var (generated, diagnostics) = TestHelpers.GetGeneratedSources<DbCommandSourceGenerator>(SourceImports + source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);
        generated.Length.ShouldBe(1);
        generated[0].ShouldContain("IDbParamsProvider");
        generated[0].ShouldContain("ToDbParams()");
        generated.ShouldNotContain(g => g.Contains("Handler"));
        generated.ShouldNotContain(g => g.Contains("HandleAsync"));
    }

    [Fact]
    public void DbCommand_TypeInfoEquality_SameType_ShouldBeEqual()
    {
        // Arrange - Run the generator twice with the same source. The incremental generator
        // caches results based on DbCommandTypeInfoSourceGen.Equals. When the model type info
        // is unchanged, the generator should produce identical output on both runs.
        var source = SourceImports + """
                                     [DbCommand(sp: "create_user", paramsCase: DbParamsCase.SnakeCase)]
                                     public partial record CreateUserCommand(int UserId, string Name) : ICommand<int>;
                                     """;

        var ct = TestContext.Current.CancellationToken;

        // Act - First run initializes the generator
        var driver = TestHelpers.CreateAndRunGenerator<DbCommandSourceGenerator>(source);
        var firstResult = driver.GetRunResult();

        // Second run with a new compilation from identical source - tests caching via Equals
        var updatedSyntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
        var updatedCompilation = CSharpCompilation.Create(
            "Platform.TestAssembly",
            [updatedSyntaxTree],
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        driver = driver.RunGenerators(updatedCompilation, ct);
        var secondResult = driver.GetRunResult();

        // Assert - Both runs should produce the same number of outputs
        firstResult.GeneratedTrees.Length.ShouldBe(secondResult.GeneratedTrees.Length);
        firstResult.GeneratedTrees.Length.ShouldBe(2); // DbParams + Handler

        // Both runs should produce identical generated code
        for (var i = 0; i < firstResult.GeneratedTrees.Length; i++)
        {
            firstResult.GeneratedTrees[i].ToString().ShouldBe(secondResult.GeneratedTrees[i].ToString());
        }

        // No errors in either run
        firstResult.Diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);
        secondResult.Diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        // Verify the DbCommandTypeInfoSourceGen Select step recognized equality (Unchanged output)
        // by checking tracked steps for the step that produces the type info model
        var trackedSteps = secondResult.Results[0].TrackedSteps;
        var hasUnchangedStep = trackedSteps
            .SelectMany(kvp => kvp.Value)
            .SelectMany(step => step.Outputs)
            .Any(output => output.Reason == IncrementalStepRunReason.Unchanged);
        hasUnchangedStep.ShouldBeTrue("Expected at least one Unchanged step output, " +
                                      "proving DbCommandTypeInfoSourceGen.Equals returned true for identical types");
    }

    [Fact]
    public void DbCommand_TypeInfoEquality_DifferentTypes_ShouldNotBeEqual()
    {
        // Arrange - Run the generator, then modify the compilation to use a different type.
        // The incremental generator should detect the change via DbCommandTypeInfoSourceGen.Equals
        // returning false, producing Modified output steps.
        var originalSource = SourceImports + """
                                             [DbCommand(sp: "create_user")]
                                             public partial record CreateUserCommand(int UserId, string Name) : ICommand<int>;
                                             """;

        var modifiedSource = SourceImports + """
                                             [DbCommand(sql: "SELECT COUNT(*) FROM users")]
                                             public partial record GetUserCountQuery() : ICommand<int>;
                                             """;

        var ct = TestContext.Current.CancellationToken;

        // Act - First run with original type
        var driver = TestHelpers.CreateAndRunGenerator<DbCommandSourceGenerator>(originalSource);
        var firstResult = driver.GetRunResult();
        firstResult.Diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        // Second run with different type
        var updatedSyntaxTree = CSharpSyntaxTree.ParseText(modifiedSource, cancellationToken: ct);
        var updatedCompilation = CSharpCompilation.Create(
            "Platform.TestAssembly",
            [updatedSyntaxTree],
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        driver = driver.RunGenerators(updatedCompilation, ct);
        var secondResult = driver.GetRunResult();

        // Assert - Output should differ since the types are different
        secondResult.Diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var firstOutput = firstResult.GeneratedTrees.Select(t => t.ToString()).ToArray();
        var secondOutput = secondResult.GeneratedTrees.Select(t => t.ToString()).ToArray();

        // The generated handler content should reference different type names
        firstOutput.ShouldContain(g => g.Contains("CreateUserCommand"));
        secondOutput.ShouldContain(g => g.Contains("GetUserCountQuery"));
        secondOutput.ShouldNotContain(g => g.Contains("CreateUserCommand"));

        // At least one step output should be Modified, proving Equals returned false
        var trackedSteps = secondResult.Results[0].TrackedSteps;
        var allOutputReasons = trackedSteps
            .SelectMany(kvp => kvp.Value)
            .SelectMany(step => step.Outputs)
            .Select(output => output.Reason)
            .ToArray();

        allOutputReasons.ShouldContain(IncrementalStepRunReason.Modified);
    }

    [Fact]
    public void DbCommand_TypeInfoEquality_WithNull_ShouldNotBeEqual()
    {
        // Arrange - Run the generator with a valid type, then modify the compilation to remove
        // the annotated type entirely. This tests the generator's handling of a type disappearing
        // (the null/missing case in the equality pipeline).
        var originalSource = SourceImports + """
                                             [DbCommand(sp: "create_user")]
                                             public partial record CreateUserCommand(int UserId, string Name) : ICommand<int>;
                                             """;

        var emptySource = SourceImports + """
                                          public record PlainRecord(int Id);
                                          """;

        var ct = TestContext.Current.CancellationToken;

        // Act - First run with annotated type
        var driver = TestHelpers.CreateAndRunGenerator<DbCommandSourceGenerator>(originalSource);
        var firstResult = driver.GetRunResult();
        firstResult.Diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);
        firstResult.GeneratedTrees.Length.ShouldBeGreaterThan(0);

        // Second run with no annotated types (equivalent to null comparison)
        var updatedSyntaxTree = CSharpSyntaxTree.ParseText(emptySource, cancellationToken: ct);
        var updatedCompilation = CSharpCompilation.Create(
            "Platform.TestAssembly",
            [updatedSyntaxTree],
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        driver = driver.RunGenerators(updatedCompilation, ct);
        var secondResult = driver.GetRunResult();

        // Assert - No output should be generated when the annotated type is removed
        secondResult.GeneratedTrees.Length.ShouldBe(0);

        // At least one step output should be Removed, proving the null/missing handling works
        var trackedSteps = secondResult.Results[0].TrackedSteps;
        var allOutputReasons = trackedSteps
            .SelectMany(kvp => kvp.Value)
            .SelectMany(step => step.Outputs)
            .Select(output => output.Reason)
            .ToArray();

        allOutputReasons.ShouldContain(IncrementalStepRunReason.Removed);
    }

    private static IEnumerable<MetadataReference> GetReferences()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Concat(
            [
                MetadataReference.CreateFromFile(typeof(DbCommandSourceGenerator).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(DbCommandAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ColumnAttribute).Assembly.Location)
            ]);
    }
}
