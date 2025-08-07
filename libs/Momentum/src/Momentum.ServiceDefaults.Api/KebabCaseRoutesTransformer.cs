// Copyright (c) Momentum .NET. All rights reserved.

using Microsoft.AspNetCore.Routing;
using Momentum.Extensions.Abstractions.Extensions;

namespace Momentum.ServiceDefaults.Api;

public sealed class KebabCaseRoutesTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        return value?.ToString()?.ToKebabCase();
    }
}
