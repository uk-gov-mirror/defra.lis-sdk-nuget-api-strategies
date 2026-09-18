// <copyright file="HttpStrategyFactoryBaseTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Http.Base;

using System.Text;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Factories;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Strategies;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

public class HttpStrategyFactoryBaseTests
{
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly IOperatorContext operatorContext = Substitute.For<IOperatorContext>();

    [Fact]
    public void FluentSetters_ShouldSetPropertiesAndReturnParent()
    {
        // Arrange
        var factory = new TestHttpStrategyFactory();
        Action<string, string?> verboseAction = (_, _) => { };

        // Act & Assert
        factory.WithDefaultApiDescription("API Description").ShouldBe(factory);
        factory.WithDefaultBaseUrl("https://example.com/api").ShouldBe(factory);
        factory.WithDefaultMediaType("application/json").ShouldBe(factory);
        factory.WithDefaultBasicAuth("admin", "pass").ShouldBe(factory);
        factory.WithDefaultVerboseOutput(verboseAction).ShouldBe(factory);

        factory.GetDefaultVerboseOutputAction().ShouldBe(verboseAction);
    }

    [Theory]
    [InlineData(null, "pass")]
    [InlineData("", "pass")]
    [InlineData(" ", "pass")]
    [InlineData("user", null)]
    [InlineData("user", "")]
    [InlineData("user", " ")]
    public void WithDefaultBasicAuth_WithInvalidInputs_ShouldThrowArgumentException(string? username, string? password)
    {
        // Arrange
        var factory = new TestHttpStrategyFactory();

        // Act & Assert
        Should.Throw<ArgumentException>(() => factory.WithDefaultBasicAuth(username!, password!));
    }

    [Fact]
    public void AttachHttpDefaultsToBuilder_WithAllDefaults_ShouldAttachAllDefaultsToBuilder()
    {
        // Arrange
        var factory = new TestHttpStrategyFactory();
        var strategy = new TestHttpStrategy();

        factory
            .WithDefaultLogger(logger)
            .WithDefaultOperatorContext(operatorContext)
            .WithDefaultApiDescription("Default Api")
            .WithDefaultBaseUrl("https://example.com")
            .WithDefaultMediaType("application/json")
            .WithDefaultBasicAuth("admin", "secret123");

        // Act
        factory.CallAttachHttpDefaultsToBuilder(strategy);

        // Assert
        strategy.GetTargetDescription().ShouldBe("Default Api");
        strategy.GetBaseUrl().ShouldBe("https://example.com");
        strategy.GetMediaType().ShouldBe("application/json");

        var expectedAuth = $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:secret123"))}";
        strategy.GetHeaders()["Authorization"].ShouldBe(expectedAuth);
    }

    [Fact]
    public void AttachHttpDefaultsToBuilder_WhenNoDefaultsConfigured_ShouldNotOverwriteBuilderProperties()
    {
        // Arrange
        var factory = new TestHttpStrategyFactory();
        var strategy = new TestHttpStrategy();

        // Act
        factory.CallAttachHttpDefaultsToBuilder(strategy);

        // Assert
        strategy.GetTargetDescription().ShouldBeNull();
        strategy.GetBaseUrl().ShouldBeNull();
        strategy.GetMediaType().ShouldBeNull();
        strategy.GetHeaders().ShouldBeEmpty();
    }
}
