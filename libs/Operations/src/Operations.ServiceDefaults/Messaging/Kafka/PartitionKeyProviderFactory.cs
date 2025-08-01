// Copyright (c) ABCDEG. All rights reserved.

using Operations.Extensions.Abstractions.Messaging;
using Operations.ServiceDefaults.Extensions;
using System.Linq.Expressions;
using System.Reflection;

namespace Operations.ServiceDefaults.Messaging.Kafka;

#pragma warning disable S3011

public static class PartitionKeyProviderFactory
{
    /// <summary>
    ///     Creates a compiled expression that efficiently retrieves the partition key as a string based on the properties marked with the
    ///     <see cref="PartitionKeyAttribute" />.
    /// </summary>
    /// <typeparam name="TMessage">The type of the object containing the partition key properties.</typeparam>
    /// <remarks>
    ///     This method is called on every event published, therefore performance is important, that is why I'm using expression trees
    ///     instead of reflection.
    ///     <para>
    ///         Generated Code:
    ///         <code>instance => {partitionKeyProperty1}.ToString() + "-" +  {partitionKeyProperty2}.ToString()...</code>
    ///     </para>
    /// </remarks>
    public static Func<TMessage, string>? GetPartitionKeyFunction<TMessage>()
    {
        var messageType = typeof(TMessage);

        var partitionKeyProperties = messageType.GetPropertiesWithAttribute<PartitionKeyAttribute>();

        if (partitionKeyProperties.Count == 0)
            return null;

        var primaryConstructor = messageType.GetPrimaryConstructor();

        var orderedPartitionKeyProperties = partitionKeyProperties
            .OrderBy(p => p.GetCustomAttribute<PartitionKeyAttribute>(primaryConstructor)?.Order ?? 0)
            .ThenBy(p => p.Name).ToArray();

        return CreatePartitionKeyGetter<TMessage>(orderedPartitionKeyProperties);
    }

    private static Func<TMessage, string> CreatePartitionKeyGetter<TMessage>(PropertyInfo[] partitionKeyProperties)
    {
        var parameter = Expression.Parameter(typeof(TMessage), "message");
        var convertToString = typeof(Convert).GetMethod(nameof(Convert.ToString), [typeof(object)])!;

        var stringValueExpressions = partitionKeyProperties
            .Select(prop => Expression.Call(convertToString, Expression.Convert(Expression.Property(parameter, prop), typeof(object))))
            .ToList();

        if (stringValueExpressions.Count == 1)
        {
            return Expression.Lambda<Func<TMessage, string>>(stringValueExpressions[0], parameter).Compile();
        }

        const string separator = "-";

        var concatMethod = typeof(string).GetMethod(nameof(string.Concat), [typeof(string[])])!;

        var expressionsWithSeparator = new List<Expression>();

        for (var i = 0; i < stringValueExpressions.Count; i++)
        {
            expressionsWithSeparator.Add(stringValueExpressions[i]);

            if (i < stringValueExpressions.Count - 1)
            {
                expressionsWithSeparator.Add(Expression.Constant(separator));
            }
        }

        var combinedExpression = Expression.Call(concatMethod, Expression.NewArrayInit(typeof(string), expressionsWithSeparator));

        return Expression.Lambda<Func<TMessage, string>>(combinedExpression, parameter).Compile();
    }
}
