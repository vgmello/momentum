// Copyright (c) Momentum .NET. All rights reserved.

using System.Reflection;

namespace Momentum.Extensions.XmlDocs;

/// <summary>
///     Defines a service for loading and retrieving XML documentation comments.
/// </summary>
public interface IXmlDocumentationService
{
    /// <summary>
    ///     Asynchronously loads XML documentation from the specified file.
    /// </summary>
    /// <param name="xmlFilePath">The path to the XML documentation file.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     <c>true</c> if the documentation was loaded successfully; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> LoadDocumentationAsync(string xmlFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the documentation for a specific member by its XML documentation name.
    /// </summary>
    /// <param name="memberName">The XML documentation member name (e.g., "T:Namespace.Type").</param>
    /// <returns>The documentation information if found; otherwise, <c>null</c>.</returns>
    XmlDocumentationInfo? GetDocumentation(string memberName);

    /// <summary>
    ///     Gets the documentation for a type.
    /// </summary>
    /// <param name="type">The type to get documentation for.</param>
    /// <returns>The documentation information if found; otherwise, <c>null</c>.</returns>
    XmlDocumentationInfo? GetTypeDocumentation(Type type);

    /// <summary>
    ///     Gets the documentation for a method.
    /// </summary>
    /// <param name="methodInfo">The method to get documentation for.</param>
    /// <returns>The documentation information if found; otherwise, <c>null</c>.</returns>
    XmlDocumentationInfo? GetMethodDocumentation(MethodInfo methodInfo);

    /// <summary>
    ///     Gets the documentation for a property.
    /// </summary>
    /// <param name="propertyInfo">The property to get documentation for.</param>
    /// <returns>The documentation information if found; otherwise, <c>null</c>.</returns>
    XmlDocumentationInfo? GetPropertyDocumentation(PropertyInfo propertyInfo);

    /// <summary>
    ///     Clears all cached documentation.
    /// </summary>
    void ClearCache();
}
