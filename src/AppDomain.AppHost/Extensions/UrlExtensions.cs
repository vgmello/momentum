// Copyright (c) ORG_NAME. All rights reserved.

namespace AppDomain.AppHost.Extensions;

public static class UrlExtensions
{
    public static IResourceBuilder<T> WithEndpointUrl<T>(this IResourceBuilder<T> builder, string endpoints, string displayText, string url = "/")
        where T : IResource
    {
        var endpointsList = endpoints.Split("|");

        builder.WithUrls(context =>
        {
            var urlForEndpoint = context.Urls.FirstOrDefault(u => endpointsList.Contains(u.Endpoint?.EndpointName));

            if (urlForEndpoint is not null)
            {
                urlForEndpoint.Url = url;
                urlForEndpoint.DisplayText = displayText;
            }
        });

        return builder;
    }
}
