// Copyright (c) ABCDEG. All rights reserved.

namespace Momentum.Extensions.XmlDocs;

public class XmlDocumentationInfo
{
    public record ParameterInfo(string? Description, string? Example);

    /// <summary>
    ///     Gets or sets the summary description of the member. This will also be used as OpenApi description
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    ///     Gets or sets the remarks for the member. This will be added to the OpenApi description if available.
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    ///     Gets or sets the return description for methods.
    /// </summary>
    public string? Returns { get; set; }

    /// <summary>
    ///     Gets or sets the parameter documentation keyed by parameter name.
    /// </summary>
    public Dictionary<string, ParameterInfo> Parameters { get; } = [];

    /// <summary>
    ///     Gets or sets the response documentation keyed by response code.
    /// </summary>
    public Dictionary<string, string?> Responses { get; } = [];

    /// <summary>
    ///     Gets or sets example usage information.
    /// </summary>
    public string? Example { get; set; }
}
