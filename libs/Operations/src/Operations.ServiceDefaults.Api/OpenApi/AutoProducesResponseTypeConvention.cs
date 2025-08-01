// Copyright (c) ABCDEG. All rights reserved.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Operations.ServiceDefaults.Api.OpenApi;

/// <summary>
///     Convention that automatically adds response type metadata to actions returning ActionResult&lt;T&gt;.
/// </summary>
/// <remarks>
///     This convention ensures that OpenAPI documentation includes proper response types for actions
///     that don't explicitly declare them via attributes. It uses a special placeholder status code
///     that is later replaced by the XML documentation transformer with the actual documented status code.
/// </remarks>
public class AutoProducesResponseTypeConvention : IActionModelConvention
{
    /// <summary>
    ///     This is a special status code that is used to indicate that the operation has an auto-generated response type.
    ///     Which should be overridden by the OpenAPI XML operation transformer.
    /// </summary>
    internal static readonly int StatusCode = -299;

    /// <summary>
    ///     Applies the convention to an action model.
    /// </summary>
    /// <param name="action">The action model to apply the convention to.</param>
    /// <remarks>
    ///     This method:
    ///     <list type="bullet">
    ///         <item>Checks if the action already has a success response type declared</item>
    ///         <item>Extracts the response type from ActionResult&lt;T&gt; return types</item>
    ///         <item>Adds a ProducesResponseTypeAttribute with a placeholder status code</item>
    ///         <item>Handles async return types (Task&lt;T&gt; and ValueTask&lt;T&gt;)</item>
    ///     </list>
    /// </remarks>
    public void Apply(ActionModel action)
    {
        var hasSuccessResponseCode = action.Filters
            .OfType<ProducesResponseTypeAttribute>()
            .Any(attr => attr.StatusCode is >= 200 and < 300);

        if (hasSuccessResponseCode)
            return;

        var returnType = action.ActionMethod.ReturnType;

        if (returnType.IsGenericType)
        {
            var genericType = returnType.GetGenericTypeDefinition();

            if (genericType == typeof(Task<>) || genericType == typeof(ValueTask<>))
            {
                returnType = returnType.GetGenericArguments()[0];
            }
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ActionResult<>))
        {
            var responseType = returnType.GetGenericArguments()[0];

            var producesAttr = new ProducesResponseTypeAttribute(responseType, StatusCode);
            action.Filters.Add(producesAttr);
        }
    }
}
