// <copyright file="HttpStrategyBaseTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Http.Base;

using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Strategies;
using Shouldly;
using Xunit;

public class HttpStrategyBaseTests
{
    [Fact]
    public void WithApiDescription_ShouldSetTargetDescriptionAndReturnParent()
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act
        var result = strategy.WithApiDescription("Test API");

        // Assert
        result.ShouldBe(strategy);

        strategy.GetTargetDescription().ShouldBe("Test API");
    }

    [Fact]
    public void WithBaseUrl_WithValidUrl_ShouldSetBaseUrlAndReturnParent()
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act
        var result = strategy.WithBaseUrl("https://example.com/api");

        // Assert
        result.ShouldBe(strategy);

        strategy.GetBaseUrl().ShouldBe("https://example.com/api");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithBaseUrl_WithInvalidUrl_ShouldThrowArgumentException(string? invalidUrl)
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act & Assert
        Should.Throw<ArgumentException>(() => strategy.WithBaseUrl(invalidUrl!));
    }

    [Fact]
    public void WithMediaType_WithValidMediaType_ShouldSetMediaTypeAndReturnParent()
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act
        var result = strategy.WithMediaType("application/xml");

        // Assert
        result.ShouldBe(strategy);

        strategy.GetMediaType().ShouldBe("application/xml");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithMediaType_WithInvalidMediaType_ShouldThrowArgumentException(string? invalidMediaType)
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act & Assert
        Should.Throw<ArgumentException>(() => strategy.WithMediaType(invalidMediaType!));
    }

    [Fact]
    public void WithHeader_WithValidValues_ShouldAddHeaderAndReturnParent()
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act
        var result = strategy.WithHeader("X-Api-Key", "Secret");

        // Assert
        result.ShouldBe(strategy);

        strategy.GetHeaders()["X-Api-Key"].ShouldBe("Secret");
    }

    [Theory]
    [InlineData(null, "value")]
    [InlineData("", "value")]
    [InlineData(" ", "value")]
    [InlineData("header", null)]
    [InlineData("header", "")]
    [InlineData("header", " ")]
    public void WithHeader_WithInvalidValues_ShouldThrowArgumentException(string? name, string? value)
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act & Assert
        Should.Throw<ArgumentException>(() => strategy.WithHeader(name!, value!));
    }

    [Fact]
    public void WithBasicAuth_WithValidCredentials_ShouldSetAuthorizationHeaderAndReturnParent()
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act
        var result = strategy.WithBasicAuth("admin", "secret123");

        // Assert
        result.ShouldBe(strategy);

        var expectedAuth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("admin:secret123"));
        strategy.GetHeaders()["Authorization"].ShouldBe(expectedAuth);
    }

    [Theory]
    [InlineData(null, "password")]
    [InlineData("", "password")]
    [InlineData(" ", "password")]
    [InlineData("username", null)]
    [InlineData("username", "")]
    [InlineData("username", " ")]
    public void WithBasicAuth_WithInvalidCredentials_ShouldThrowArgumentException(string? username, string? password)
    {
        // Arrange
        var strategy = new TestHttpStrategy();

        // Act & Assert
        Should.Throw<ArgumentException>(() => strategy.WithBasicAuth(username!, password!));
    }
}
