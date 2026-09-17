// <copyright file="SoapStrategyFactoryTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations;

using System.Reflection;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Soap.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

public class SoapStrategyFactoryTests
{
    private readonly IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
    private readonly ISoapHttpClient soapHttpClient = Substitute.For<ISoapHttpClient>();
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly IOperatorContext operatorContext = Substitute.For<IOperatorContext>();

    public SoapStrategyFactoryTests()
    {
        serviceProvider.GetService(typeof(ISoapHttpClient)).Returns(soapHttpClient);
    }

    [Fact]
    public void FluentSetters_ShouldSetDefaultsAndReturnFactory()
    {
        // Arrange
        var factory = new SoapStrategyFactory<TestService>(serviceProvider);

        // Act & Assert
        factory.WithDefaultApiDescription("Sample Soap Api").ShouldBe(factory);
        factory.WithDefaultBaseUrl("https://example.com/soap").ShouldBe(factory);
        factory.WithDefaultServiceUrl("service").ShouldBe(factory);
        factory.WithDefaultSoapAction("urn:action").ShouldBe(factory);
        factory.WithDefaultMediaType("text/xml").ShouldBe(factory);
        factory.WithDefaultBasicAuth("admin", "secret").ShouldBe(factory);
        factory.WithDefaultXmlDeclaration(true).ShouldBe(factory);
        factory.WithDefaultVerboseOutput((_, _) => { }).ShouldBe(factory);
    }

    [Fact]
    public void BuildSoapStrategy_ShouldCreateStrategyAndAttachAllDefaults()
    {
        // Arrange
        var verboseCalled = false;
        var factory = new SoapStrategyFactory<TestService>(serviceProvider);

        factory
            .WithDefaultLogger(logger)
            .WithDefaultOperatorContext(operatorContext)
            .WithDefaultApiDescription("Sample Soap Api")
            .WithDefaultBaseUrl("https://example.com/soap")
            .WithDefaultServiceUrl("service")
            .WithDefaultSoapAction("urn:action")
            .WithDefaultMediaType("text/xml")
            .WithDefaultBasicAuth("admin", "secret123")
            .WithDefaultXmlDeclaration(true)
            .WithDefaultVerboseOutput((_, _) => verboseCalled = true);

        // Act
        var strategy = factory.BuildSoapStrategy();

        // Assert
        strategy.ShouldNotBeNull();
        strategy.ShouldBeOfType<SoapStrategy<TestService>>();

        var soapStrategy = (SoapStrategy<TestService>)strategy;

        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        typeof(SoapStrategy<TestService>).GetProperty("Logger", flags)?.GetValue(soapStrategy).ShouldBe(logger);

        typeof(SoapStrategy<TestService>).GetProperty("TargetDescription", flags)?.GetValue(soapStrategy)
            .ShouldBe("Sample Soap Api");

        typeof(SoapStrategy<TestService>).GetProperty("BaseUrl", flags)?.GetValue(soapStrategy)
            .ShouldBe("https://example.com/soap");

        typeof(SoapStrategy<TestService>).GetProperty("ServiceUrl", flags)?.GetValue(soapStrategy).ShouldBe("service");

        typeof(SoapStrategy<TestService>).GetProperty("SoapAction", flags)?.GetValue(soapStrategy)
            .ShouldBe("urn:action");

        typeof(SoapStrategy<TestService>).GetProperty("MediaType", flags)?.GetValue(soapStrategy).ShouldBe("text/xml");

        var headers = (Dictionary<string, string>?)typeof(SoapStrategy<TestService>)
            .GetProperty("Headers", flags)?.GetValue(soapStrategy);
        headers.ShouldNotBeNull();
        var expectedAuth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("admin:secret123"));
        headers["Authorization"].ShouldBe(expectedAuth);

        typeof(SoapStrategy<TestService>).GetProperty("IncludeXmlDeclaration", flags)?.GetValue(soapStrategy)
            .ShouldBe(true);

        var verboseAction = (Action<string, string?>?)typeof(SoapStrategy<TestService>)
            .GetProperty("VerboseOutputAction", flags)?.GetValue(soapStrategy);

        verboseAction.ShouldNotBeNull();
        verboseAction?.Invoke("test", "data");
        verboseCalled.ShouldBeTrue();
    }

    [Fact]
    public void BuildSoapStrategy_WhenDefaultsNotConfigured_ShouldCreateStrategyWithoutDefaults()
    {
        // Arrange
        var factory = new SoapStrategyFactory<TestService>(serviceProvider);

        // Act
        var strategy = factory.BuildSoapStrategy();

        // Assert
        strategy.ShouldNotBeNull();
        strategy.ShouldBeOfType<SoapStrategy<TestService>>();

        var soapStrategy = (SoapStrategy<TestService>)strategy;

        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        typeof(SoapStrategy<TestService>).GetProperty("Logger", flags)?.GetValue(soapStrategy).ShouldBeNull();

        typeof(SoapStrategy<TestService>).GetProperty("TargetDescription", flags)?.GetValue(soapStrategy)
            .ShouldBeNull();

        typeof(SoapStrategy<TestService>).GetProperty("BaseUrl", flags)?.GetValue(soapStrategy).ShouldBeNull();

        typeof(SoapStrategy<TestService>).GetProperty("ServiceUrl", flags)?.GetValue(soapStrategy).ShouldBeNull();

        typeof(SoapStrategy<TestService>).GetProperty("SoapAction", flags)?.GetValue(soapStrategy).ShouldBeNull();

        typeof(SoapStrategy<TestService>).GetProperty("MediaType", flags)?.GetValue(soapStrategy).ShouldBeNull();

        typeof(SoapStrategy<TestService>).GetProperty("IncludeXmlDeclaration", flags)?.GetValue(soapStrategy)
            .ShouldBe(false);

        typeof(SoapStrategy<TestService>).GetProperty("VerboseOutputAction", flags)?.GetValue(soapStrategy)
            .ShouldBeNull();
    }
}
