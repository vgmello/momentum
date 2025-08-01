// Copyright (c) ABCDEG. All rights reserved.

using Microsoft.AspNetCore.Routing;
using Operations.Extensions.Abstractions.Extensions;

namespace Operations.ServiceDefaults.Api;

public sealed class KebabCaseRoutesTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        return value?.ToString()?.ToKebabCase();
    }
}
