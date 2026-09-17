// <copyright file="SoapStrategyFactory.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Soap;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Soap.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap;
using Microsoft.Extensions.DependencyInjection;

public sealed class SoapStrategyFactory<TService> : HttpStrategyFactoryBase<TService, ISoapStrategyFactory<TService>>,
    ISoapStrategyFactory<TService>
    where TService : class
{
    private readonly IServiceProvider serviceProvider;

    public SoapStrategyFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        SetParentFactory(this);
    }

    private string? DefaultServiceUrl { get; set; }

    private string? DefaultSoapAction { get; set; }

    private bool? DefaultXmlDeclaration { get; set; }

    public ISoapStrategyFactory<TService> WithDefaultServiceUrl(string serviceUrl)
    {
        DefaultServiceUrl = serviceUrl;
        return this;
    }

    public ISoapStrategyFactory<TService> WithDefaultSoapAction(string soapAction)
    {
        DefaultSoapAction = soapAction;
        return this;
    }

    public ISoapStrategyFactory<TService> WithDefaultXmlDeclaration(bool withDefaultXmlDeclaration)
    {
        DefaultXmlDeclaration = withDefaultXmlDeclaration;
        return this;
    }

    public ISoapStrategy<TService> BuildSoapStrategy()
    {
        var soapHttpClient = serviceProvider.GetRequiredService<ISoapHttpClient>();

        var soapStrategy = new SoapStrategy<TService>(soapHttpClient);

        AttachDefaults(soapStrategy);

        return soapStrategy;
    }

    private void AttachDefaults(SoapStrategy<TService> strategyBuilder)
    {
        AttachHttpDefaultsToBuilder(strategyBuilder);

        if (DefaultServiceUrl != null)
        {
            strategyBuilder.WithServiceUrl(DefaultServiceUrl);
        }

        if (DefaultSoapAction != null)
        {
            strategyBuilder.WithSoapAction(DefaultSoapAction);
        }

        if (DefaultXmlDeclaration != null)
        {
            strategyBuilder.WithXmlDeclaration(DefaultXmlDeclaration.Value);
        }

        if (DefaultVerboseOutputAction != null)
        {
            strategyBuilder.WithVerboseOutput(DefaultVerboseOutputAction);
        }
    }
}
