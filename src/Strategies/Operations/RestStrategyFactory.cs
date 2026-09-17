// <copyright file="RestStrategyFactory.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations;

using System.Text.Json;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest;
using Microsoft.Extensions.DependencyInjection;

public sealed class RestStrategyFactory<TService> : HttpStrategyFactoryBase<TService, IRestStrategyFactory<TService>>,
    IRestStrategyFactory<TService>
    where TService : class
{
    private readonly IServiceProvider serviceProvider;

    public RestStrategyFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        SetParentFactory(this);
    }

    private string? DefaultResourceUrl { get; set; }

    private JsonSerializerOptions? DefaultJsonSerializerOptions { get; set; }

    public IRestStrategyFactory<TService> WithDefaultResourceUrl(string resourceUrl)
    {
        DefaultResourceUrl = resourceUrl;
        return this;
    }

    public IRestStrategyFactory<TService> WithDefaultJsonSerializerOptions(JsonSerializerOptions jsonSerializerOptions)
    {
        DefaultJsonSerializerOptions = jsonSerializerOptions;
        return this;
    }

    public IRestStrategy<TService> BuildRestStrategy()
    {
        var restHttpClient = serviceProvider.GetRequiredService<IRestHttpClient>();

        var restStrategy = new RestStrategy<TService>(restHttpClient);

        AttachDefaults(restStrategy);

        return restStrategy;
    }

    private void AttachDefaults(RestStrategy<TService> strategyBuilder)
    {
        AttachHttpDefaultsToBuilder(strategyBuilder);

        if (DefaultResourceUrl != null)
        {
            strategyBuilder.WithResourceUrl(DefaultResourceUrl);
        }

        if (DefaultJsonSerializerOptions != null)
        {
            strategyBuilder.WithJsonSerializerOptions(DefaultJsonSerializerOptions);
        }

        if (DefaultVerboseOutputAction != null)
        {
            strategyBuilder.WithVerboseOutput(DefaultVerboseOutputAction);
        }
    }
}
