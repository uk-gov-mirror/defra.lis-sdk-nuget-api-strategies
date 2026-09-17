// <copyright file="RestStrategyFactoryTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations;

using System.Reflection;
using System.Text.Json;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

public class RestStrategyFactoryTests
{
    private readonly IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IRestHttpClient restHttpClient = Substitute.For<IRestHttpClient>();
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly IOperatorContext operatorContext = Substitute.For<IOperatorContext>();

    public RestStrategyFactoryTests()
    {
        serviceProvider.GetService(typeof(IRestHttpClient)).Returns(restHttpClient);
    }

    [Fact]
    public void FluentSetters_ShouldSetDefaultsAndReturnFactory()
    {
        // Arrange
        var factory = new RestStrategyFactory<TestService>(serviceProvider);
        var options = new JsonSerializerOptions();

        // Act & Assert
        factory.WithDefaultApiDescription("Sample Rest Api").ShouldBe(factory);
        factory.WithDefaultBaseUrl("https://example.com/api").ShouldBe(factory);
        factory.WithDefaultResourceUrl("users").ShouldBe(factory);
        factory.WithDefaultMediaType("application/json").ShouldBe(factory);
        factory.WithDefaultBasicAuth("admin", "secret").ShouldBe(factory);
        factory.WithDefaultJsonSerializerOptions(options).ShouldBe(factory);
        factory.WithDefaultVerboseOutput((_, _) => { }).ShouldBe(factory);
    }

    [Fact]
    public void BuildRestStrategy_ShouldCreateStrategyAndAttachAllDefaults()
    {
        // Arrange
        var verboseCalled = false;
        var factory = new RestStrategyFactory<TestService>(serviceProvider);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower };

        factory
            .WithDefaultLogger(logger)
            .WithDefaultOperatorContext(operatorContext)
            .WithDefaultApiDescription("Sample Rest Api")
            .WithDefaultBaseUrl("https://example.com/api")
            .WithDefaultResourceUrl("users")
            .WithDefaultMediaType("application/vnd.api+json")
            .WithDefaultBasicAuth("admin", "secret123")
            .WithDefaultJsonSerializerOptions(options)
            .WithDefaultVerboseOutput((_, _) => verboseCalled = true);

        // Act
        var strategy = factory.BuildRestStrategy();

        // Assert
        strategy.ShouldNotBeNull();
        strategy.ShouldBeOfType<RestStrategy<TestService>>();

        var restStrategy = (RestStrategy<TestService>)strategy;

        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        typeof(RestStrategy<TestService>).GetProperty("Logger", flags)?.GetValue(restStrategy).ShouldBe(logger);

        typeof(RestStrategy<TestService>).GetProperty("TargetDescription", flags)?.GetValue(restStrategy)
            .ShouldBe("Sample Rest Api");

        typeof(RestStrategy<TestService>).GetProperty("BaseUrl", flags)?.GetValue(restStrategy)
            .ShouldBe("https://example.com/api");

        typeof(RestStrategy<TestService>).GetProperty("ResourceUrl", flags)?.GetValue(restStrategy)
            .ShouldBe("users");

        typeof(RestStrategy<TestService>).GetProperty("MediaType", flags)?.GetValue(restStrategy)
            .ShouldBe("application/vnd.api+json");

        var headers = (Dictionary<string, string>?)typeof(RestStrategy<TestService>)
            .GetProperty("Headers", flags)?.GetValue(restStrategy);
        headers.ShouldNotBeNull();
        var expectedAuth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("admin:secret123"));
        headers["Authorization"].ShouldBe(expectedAuth);

        typeof(RestStrategy<TestService>).GetProperty("JsonSerializerOptions", flags)?.GetValue(restStrategy)
            .ShouldBe(options);

        var verboseAction = (Action<string, string?>?)typeof(RestStrategy<TestService>)
            .GetProperty("VerboseOutputAction", flags)?.GetValue(restStrategy);

        verboseAction.ShouldNotBeNull();
        verboseAction?.Invoke("test", "data");
        verboseCalled.ShouldBeTrue();
    }

    [Fact]
    public void BuildRestStrategy_WhenDefaultsNotConfigured_ShouldCreateStrategyWithoutCustomDefaults()
    {
        // Arrange
        var factory = new RestStrategyFactory<TestService>(serviceProvider);

        // Act
        var strategy = factory.BuildRestStrategy();

        // Assert
        strategy.ShouldNotBeNull();
        strategy.ShouldBeOfType<RestStrategy<TestService>>();

        var restStrategy = (RestStrategy<TestService>)strategy;

        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        typeof(RestStrategy<TestService>).GetProperty("Logger", flags)?.GetValue(restStrategy).ShouldBeNull();

        typeof(RestStrategy<TestService>).GetProperty("TargetDescription", flags)?.GetValue(restStrategy)
            .ShouldBeNull();

        typeof(RestStrategy<TestService>).GetProperty("BaseUrl", flags)?.GetValue(restStrategy).ShouldBeNull();

        typeof(RestStrategy<TestService>).GetProperty("ResourceUrl", flags)?.GetValue(restStrategy).ShouldBeNull();

        typeof(RestStrategy<TestService>).GetProperty("MediaType", flags)?.GetValue(restStrategy)
            .ShouldBe("application/json");

        typeof(RestStrategy<TestService>).GetProperty("VerboseOutputAction", flags)?.GetValue(restStrategy)
            .ShouldBeNull();
    }
}
