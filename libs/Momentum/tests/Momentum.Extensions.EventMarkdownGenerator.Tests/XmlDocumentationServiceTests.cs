// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.Extensions.Logging.Abstractions;
using Momentum.Extensions.XmlDocs;
using Shouldly;
using Xunit;

namespace Momentum.Extensions.EventMarkdownGenerator.Tests;

public class XmlDocumentationServiceTests
{
    private readonly XmlDocumentationService _service = new(NullLogger<XmlDocumentationService>.Instance);

    private static string CreateTempXmlFile(string xmlContent)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, xmlContent);

        return tempFile;
    }

    private static string WrapInDoc(string membersContent) =>
        $"""
         <?xml version="1.0"?>
         <doc>
           <members>
             {membersContent}
           </members>
         </doc>
         """;

    [Fact]
    public async Task LoadDocumentationAsync_WithValidXml_ShouldReturnTrue()
    {
        var xml = WrapInDoc("""
            <member name="T:MyNamespace.MyClass">
              <summary>My summary</summary>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            var result = await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            result.ShouldBeTrue();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDocumentationAsync_WithNonExistentFile_ShouldReturnFalse()
    {
        var result = await _service.LoadDocumentationAsync("/non/existent/path/file.xml", TestContext.Current.CancellationToken);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task LoadDocumentationAsync_WithSummary_ShouldParseSummary()
    {
        var xml = WrapInDoc("""
            <member name="T:MyNamespace.MyClass">
              <summary>This is the summary text</summary>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var doc = _service.GetDocumentation("T:MyNamespace.MyClass");
            doc.ShouldNotBeNull();
            doc.Summary.ShouldBe("This is the summary text");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDocumentationAsync_WithRemarks_ShouldParseRemarks()
    {
        var xml = WrapInDoc("""
            <member name="T:MyNamespace.MyClass">
              <summary>Summary</summary>
              <remarks>These are the remarks</remarks>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var doc = _service.GetDocumentation("T:MyNamespace.MyClass");
            doc.ShouldNotBeNull();
            doc.Remarks.ShouldBe("These are the remarks");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDocumentationAsync_WithReturns_ShouldParseReturns()
    {
        var xml = WrapInDoc("""
            <member name="M:MyNamespace.MyClass.MyMethod">
              <summary>Summary</summary>
              <returns>The return value description</returns>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var doc = _service.GetDocumentation("M:MyNamespace.MyClass.MyMethod");
            doc.ShouldNotBeNull();
            doc.Returns.ShouldBe("The return value description");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDocumentationAsync_WithParam_ShouldParseParameters()
    {
        var xml = WrapInDoc("""
            <member name="M:MyNamespace.MyClass.MyMethod(System.String)">
              <summary>Summary</summary>
              <param name="name">The name of the entity</param>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var doc = _service.GetDocumentation("M:MyNamespace.MyClass.MyMethod(System.String)");
            doc.ShouldNotBeNull();
            doc.Parameters.ShouldContainKey("name");
            doc.Parameters["name"].Description.ShouldBe("The name of the entity");
            doc.Parameters["name"].Example.ShouldBeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDocumentationAsync_WithParamExample_ShouldParseExample()
    {
        var xml = WrapInDoc("""
            <member name="M:MyNamespace.MyClass.MyMethod(System.String)">
              <summary>Summary</summary>
              <param name="name" example="John Doe">The name of the entity</param>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var doc = _service.GetDocumentation("M:MyNamespace.MyClass.MyMethod(System.String)");
            doc.ShouldNotBeNull();
            doc.Parameters["name"].Description.ShouldBe("The name of the entity");
            doc.Parameters["name"].Example.ShouldBe("John Doe");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDocumentationAsync_WithResponse_ShouldParseResponses()
    {
        var xml = WrapInDoc("""
            <member name="M:MyNamespace.MyClass.MyMethod">
              <summary>Summary</summary>
              <response code="200">Success response</response>
              <response code="404">Not found response</response>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var doc = _service.GetDocumentation("M:MyNamespace.MyClass.MyMethod");
            doc.ShouldNotBeNull();
            doc.Responses.ShouldContainKey("200");
            doc.Responses["200"].ShouldBe("Success response");
            doc.Responses.ShouldContainKey("404");
            doc.Responses["404"].ShouldBe("Not found response");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDocumentationAsync_WithExample_ShouldParseExample()
    {
        var xml = WrapInDoc("""
            <member name="T:MyNamespace.MyClass">
              <summary>Summary</summary>
              <example>var x = new MyClass();</example>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var doc = _service.GetDocumentation("T:MyNamespace.MyClass");
            doc.ShouldNotBeNull();
            doc.Example.ShouldBe("var x = new MyClass();");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDocumentationAsync_WithEmptyMember_ShouldReturnNull()
    {
        var xml = WrapInDoc("""<member name="T:MyNamespace.Empty" />""");
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var doc = _service.GetDocumentation("T:MyNamespace.Empty");
            doc.ShouldBeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDocumentationAsync_WithUnknownElement_ShouldNotFail()
    {
        var xml = WrapInDoc("""
            <member name="T:MyNamespace.MyClass">
              <summary>Summary</summary>
              <customtag>Some custom content</customtag>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            var result = await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            result.ShouldBeTrue();
            var doc = _service.GetDocumentation("T:MyNamespace.MyClass");
            doc.ShouldNotBeNull();
            doc.Summary.ShouldBe("Summary");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDocumentationAsync_WithMemberWithNoRecognizedContent_ShouldReturnNull()
    {
        var xml = WrapInDoc("""
            <member name="T:MyNamespace.OnlyUnknown">
              <unknowntag>Something</unknowntag>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var doc = _service.GetDocumentation("T:MyNamespace.OnlyUnknown");
            doc.ShouldBeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetDocumentation_WithUnknownMember_ShouldReturnNull()
    {
        var doc = _service.GetDocumentation("T:NonExistent.Type");

        doc.ShouldBeNull();
    }

    [Fact]
    public async Task ClearCache_ShouldRemoveAllEntries()
    {
        var xml = WrapInDoc("""
            <member name="T:MyNamespace.MyClass">
              <summary>Summary</summary>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);
            _service.GetDocumentation("T:MyNamespace.MyClass").ShouldNotBeNull();

            _service.ClearCache();

            _service.GetDocumentation("T:MyNamespace.MyClass").ShouldBeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetTypeDocumentation_ShouldUseTPrefix()
    {
        var xml = WrapInDoc("""
            <member name="T:System.String">
              <summary>String type summary</summary>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var doc = _service.GetTypeDocumentation(typeof(string));

            doc.ShouldNotBeNull();
            doc.Summary.ShouldBe("String type summary");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetPropertyDocumentation_ShouldUsePPrefix()
    {
        var fullName = typeof(TestClassForDocumentation).FullName;
        var xml = WrapInDoc($"""
            <member name="P:{fullName}.Name">
              <summary>The name property</summary>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var property = typeof(TestClassForDocumentation).GetProperty(nameof(TestClassForDocumentation.Name))!;
            var doc = _service.GetPropertyDocumentation(property);

            doc.ShouldNotBeNull();
            doc.Summary.ShouldBe("The name property");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetMethodDocumentation_WithNoParameters_ShouldUseMPrefix()
    {
        var fullName = typeof(TestClassForDocumentation).FullName;
        var xml = WrapInDoc($"""
            <member name="M:{fullName}.DoSomething">
              <summary>Does something</summary>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var method = typeof(TestClassForDocumentation).GetMethod(nameof(TestClassForDocumentation.DoSomething))!;
            var doc = _service.GetMethodDocumentation(method);

            doc.ShouldNotBeNull();
            doc.Summary.ShouldBe("Does something");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetMethodDocumentation_WithParameters_ShouldIncludeParameterTypes()
    {
        var fullName = typeof(TestClassForDocumentation).FullName;
        var xml = WrapInDoc($"""
            <member name="M:{fullName}.DoSomethingWithArgs(System.String,System.Int32)">
              <summary>Does something with args</summary>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var method = typeof(TestClassForDocumentation).GetMethod(nameof(TestClassForDocumentation.DoSomethingWithArgs))!;
            var doc = _service.GetMethodDocumentation(method);

            doc.ShouldNotBeNull();
            doc.Summary.ShouldBe("Does something with args");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetMethodDocumentation_WithGenericParameter_ShouldFormatCorrectly()
    {
        var fullName = typeof(TestClassForDocumentation).FullName;
        var memberName = $"M:{fullName}.DoSomethingWithList(System.Collections.Generic.List" + "{System.String})";
        var xml = WrapInDoc(
            $"""
            <member name="{memberName}">
              <summary>Does something with list</summary>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var method = typeof(TestClassForDocumentation).GetMethod(nameof(TestClassForDocumentation.DoSomethingWithList))!;
            var doc = _service.GetMethodDocumentation(method);

            doc.ShouldNotBeNull();
            doc.Summary.ShouldBe("Does something with list");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDocumentationAsync_WithAllElements_ShouldParseAll()
    {
        var xml = WrapInDoc("""
            <member name="M:MyNamespace.MyClass.CompleteMethod(System.String)">
              <summary>Complete summary</summary>
              <remarks>Complete remarks</remarks>
              <returns>Complete returns</returns>
              <param name="input" example="hello">The input parameter</param>
              <response code="200">OK</response>
              <example>CompleteMethod("test")</example>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var doc = _service.GetDocumentation("M:MyNamespace.MyClass.CompleteMethod(System.String)");
            doc.ShouldNotBeNull();
            doc.Summary.ShouldBe("Complete summary");
            doc.Remarks.ShouldBe("Complete remarks");
            doc.Returns.ShouldBe("Complete returns");
            doc.Parameters["input"].Description.ShouldBe("The input parameter");
            doc.Parameters["input"].Example.ShouldBe("hello");
            doc.Responses["200"].ShouldBe("OK");
            doc.Example.ShouldBe("CompleteMethod(\"test\")");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDocumentationAsync_WithEmptySummary_ShouldReturnEntryWithNullSummary()
    {
        var xml = WrapInDoc("""
            <member name="T:MyNamespace.MyClass">
              <summary />
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var doc = _service.GetDocumentation("T:MyNamespace.MyClass");
            doc.ShouldNotBeNull();
            doc.Summary.ShouldBeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDocumentationAsync_WithMultipleMembers_ShouldParseAll()
    {
        var xml = WrapInDoc("""
            <member name="T:MyNamespace.ClassA">
              <summary>Class A summary</summary>
            </member>
            <member name="T:MyNamespace.ClassB">
              <summary>Class B summary</summary>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            _service.GetDocumentation("T:MyNamespace.ClassA")!.Summary.ShouldBe("Class A summary");
            _service.GetDocumentation("T:MyNamespace.ClassB")!.Summary.ShouldBe("Class B summary");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDocumentationAsync_WithInvalidXml_ShouldReturnFalse()
    {
        var tempFile = CreateTempXmlFile("this is not valid xml <><>");

        try
        {
            var result = await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            result.ShouldBeFalse();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetTypeDocumentation_WithUnknownType_ShouldReturnNull()
    {
        var xml = WrapInDoc("""
            <member name="T:MyNamespace.SomeOtherType">
              <summary>Summary</summary>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var doc = _service.GetTypeDocumentation(typeof(int));

            doc.ShouldBeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDocumentationAsync_WithResponseMissingCode_ShouldSkipResponse()
    {
        var xml = WrapInDoc("""
            <member name="M:MyNamespace.MyClass.MyMethod">
              <summary>Summary</summary>
              <response>Response without code attribute</response>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var doc = _service.GetDocumentation("M:MyNamespace.MyClass.MyMethod");
            doc.ShouldNotBeNull();
            doc.Responses.Count.ShouldBe(0);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadDocumentationAsync_WithParamMissingName_ShouldSkipParam()
    {
        var xml = WrapInDoc("""
            <member name="M:MyNamespace.MyClass.MyMethod">
              <summary>Summary</summary>
              <param>Parameter without name attribute</param>
            </member>
            """);
        var tempFile = CreateTempXmlFile(xml);

        try
        {
            await _service.LoadDocumentationAsync(tempFile, TestContext.Current.CancellationToken);

            var doc = _service.GetDocumentation("M:MyNamespace.MyClass.MyMethod");
            doc.ShouldNotBeNull();
            doc.Parameters.Count.ShouldBe(0);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Test helper type used by reflection-based tests.
    // Methods must remain instance methods so MethodInfo.DeclaringType resolves correctly.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1822:Mark members as static")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2325:Make member static")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1186:Add nested comment or throw")]
    public class TestClassForDocumentation
    {
        public string Name { get; set; } = "";

        public void DoSomething() { /* Intentionally empty - used for reflection tests */ }

        public void DoSomethingWithArgs(string name, int count)
        {
            _ = name;
            _ = count;
        }

        public void DoSomethingWithList(List<string> items)
        {
            _ = items;
        }
    }
}
