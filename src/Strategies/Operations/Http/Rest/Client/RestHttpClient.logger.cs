// <copyright file="RestHttpClient.logger.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Client;

using Microsoft.Extensions.Logging;

/// <summary>
/// Logging operations for the rest http client.
/// </summary>
public sealed partial class RestHttpClient
{
    [LoggerMessage(LogLevel.Information,
        "Calling REST endpoint '{method}' '{absoluteUrl}' ...")]
    static partial void LogCallingRestEndpoint(ILogger<RestHttpClient> logger, string method, string absoluteUrl);

    [LoggerMessage(LogLevel.Information,
        "Successfully called REST endpoint '{method}' '{absoluteUrl}'")]
    static partial void LogSuccessfullyCalledRestEndpoint(ILogger<RestHttpClient> logger, string method, string absoluteUrl);
}
