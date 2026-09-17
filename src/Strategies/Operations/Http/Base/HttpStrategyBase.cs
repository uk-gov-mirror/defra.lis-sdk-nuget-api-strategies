// <copyright file="HttpStrategyBase.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Base;

using System.Text;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Base;

/// <summary>
/// Base class for http strategy builders.
/// </summary>
/// <typeparam name="TService">The type of the service class that is consuming the derived builder.</typeparam>
/// <typeparam name="TParent">The type of the derived builder.</typeparam>
public abstract class HttpStrategyBase<TService, TParent> : StrategyBase<TService, TParent>,
    IHttpStrategy<TService, TParent>
    where TService : class
    where TParent : class
{
    protected string? BaseUrl { get; private set; }

    protected Dictionary<string, string> Headers { get; } = new();

    protected string? MediaType { get; private set; }

    public TParent WithApiDescription(string apiDescription)
    {
        TargetDescription = apiDescription;
        return GetParentBuilder();
    }

    public TParent WithBaseUrl(string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        BaseUrl = baseUrl;

        return GetParentBuilder();
    }

    public TParent WithHeader(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        Headers.Add(name, value);

        return GetParentBuilder();
    }

    public TParent WithMediaType(string mediaType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);

        MediaType = mediaType;

        return GetParentBuilder();
    }

    public TParent WithBasicAuth(string username, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));

        Headers["Authorization"] = encoded;

        return GetParentBuilder();
    }
}
