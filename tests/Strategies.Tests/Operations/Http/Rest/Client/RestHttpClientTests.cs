// <copyright file="RestHttpClientTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Http.Rest.Client;

using System.Net;
using System.Text;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Handlers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

public class RestHttpClientTests
{
    private readonly ILogger<RestHttpClient> logger = Substitute.For<ILogger<RestHttpClient>>();

    public RestHttpClientTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    [Fact]
    public void FluentSetters_WithValidInputs_ShouldReturnInstance()
    {
        // Arrange
        var httpClient = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new RestHttpClient(httpClient, logger);

        // Act & Assert
        client.WithBaseUrl("https://example.com/api").ShouldBe(client);
        client.WithMediaType("application/json").ShouldBe(client);
        client.WithHeader("X-Custom", "Value").ShouldBe(client);
        client.WithHeader("X-Custom", "OverwrittenValue").ShouldBe(client);
        client.WithQueryParameter("page", "1").ShouldBe(client);
        client.WithVerboseOutput((_, _) => { }).ShouldBe(client);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithBaseUrl_WithInvalidValue_ShouldThrowArgumentException(string? baseUrl)
    {
        // Arrange
        var httpClient = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new RestHttpClient(httpClient, logger);

        // Act & Assert
        Should.Throw<ArgumentException>(() => client.WithBaseUrl(baseUrl!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithMediaType_WithInvalidValue_ShouldThrowArgumentException(string? mediaType)
    {
        // Arrange
        var httpClient = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new RestHttpClient(httpClient, logger);

        // Act & Assert
        Should.Throw<ArgumentException>(() => client.WithMediaType(mediaType!));
    }

    [Theory]
    [InlineData(null, "value")]
    [InlineData("", "value")]
    [InlineData(" ", "value")]
    [InlineData("name", null)]
    [InlineData("name", "")]
    [InlineData("name", " ")]
    public void WithHeader_WithInvalidValue_ShouldThrowArgumentException(string? name, string? value)
    {
        // Arrange
        var httpClient = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new RestHttpClient(httpClient, logger);

        // Act & Assert
        Should.Throw<ArgumentException>(() => client.WithHeader(name!, value!));
    }

    [Theory]
    [InlineData(null, "value")]
    [InlineData("", "value")]
    [InlineData(" ", "value")]
    [InlineData("name", null)]
    [InlineData("name", "")]
    [InlineData("name", " ")]
    public void WithQueryParameter_WithInvalidValue_ShouldThrowArgumentException(string? name, string? value)
    {
        // Arrange
        var httpClient = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new RestHttpClient(httpClient, logger);

        // Act & Assert
        Should.Throw<ArgumentException>(() => client.WithQueryParameter(name!, value!));
    }

    [Fact]
    public async Task SendAsync_WithoutBaseUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var httpClient = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new RestHttpClient(httpClient, logger);
        client.WithMediaType("application/json");

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => client.SendAsync(HttpMethod.Get, "/test"));
        ex.Message.ShouldBe("Base URL must be set before making a rest request.");
    }

    [Fact]
    public async Task SendAsync_WithoutMediaType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var httpClient = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new RestHttpClient(httpClient, logger);
        client.WithBaseUrl("https://example.com/api");

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => client.SendAsync(HttpMethod.Get, "/test"));
        ex.Message.ShouldBe("Media type must be set before making a rest request.");
    }

    [Fact]
    public async Task SendAsync_WithValidRequestAndPayload_ExecutesSuccessfullyAndLogs()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedRequestBody = null;

        var handler = new TestHttpMessageHandler(req =>
        {
            capturedRequest = req;
            if (req.Content != null)
            {
                capturedRequestBody = req.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json"),
            };
        });

        var verboseLogs = new List<(string, string?)>();
        var httpClient = new HttpClient(handler);
        var client = new RestHttpClient(httpClient, logger);

        client.WithBaseUrl("https://example.com/api/")
            .WithMediaType("application/json")
            .WithHeader("X-Api-Key", "secret-key")
            .WithQueryParameter("limit", "10")
            .WithVerboseOutput((desc, data) => verboseLogs.Add((desc, data)));

        // Act
        var response = await client.SendAsync(
            HttpMethod.Post,
            "users/create",
            "{\"name\":\"John\"}",
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldNotBeNull();
        response.ShouldSatisfyAllConditions(
            x => x.StatusCode.ShouldBe(HttpStatusCode.OK),
            x => x.HasContent.ShouldBeTrue(),
            x => x.Content.ShouldBe("{\"status\":\"ok\"}"));

        capturedRequest.ShouldNotBeNull();
        capturedRequest.ShouldSatisfyAllConditions(
            x => x.Method.ShouldBe(HttpMethod.Post),
            x => x.RequestUri.ShouldBe(new Uri("https://example.com/api/users/create?limit=10")),
            x => x.Headers.GetValues("X-Api-Key").ShouldContain("secret-key"),
            x => x.Headers.Accept.ToString().ShouldBe("application/json"));

        capturedRequestBody.ShouldBe("{\"name\":\"John\"}");

        verboseLogs.ShouldContain(x => x.Item1 == "Created Request Payload:" && x.Item2 == "{\"name\":\"John\"}");
        verboseLogs.ShouldContain(x =>
            x.Item1 == "Sending POST Request to:" && x.Item2 == "https://example.com/api/users/create?limit=10");
        verboseLogs.ShouldContain(x =>
            x.Item1 == "Successfully sent POST Request to:" &&
            x.Item2 == "https://example.com/api/users/create?limit=10");

        logger.ShouldHaveReceived(
            LogLevel.Information,
            "Calling REST endpoint 'POST' 'https://example.com/api/users/create?limit=10' ...");

        logger.ShouldHaveReceived(
            LogLevel.Information,
            "Successfully called REST endpoint 'POST' 'https://example.com/api/users/create?limit=10'");
    }

    [Fact]
    public async Task SendAsync_WhenRelativeUrlHasQueryAndParametersAdded_AppendsAmpersand()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var handler = new TestHttpMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            };
        });

        var httpClient = new HttpClient(handler);
        var client = new RestHttpClient(httpClient, logger);

        client.WithBaseUrl("https://example.com/api")
            .WithMediaType("application/json")
            .WithQueryParameter("filter", "active");

        // Act
        await client.SendAsync(HttpMethod.Get, "items?type=all", null, TestContext.Current.CancellationToken);

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.RequestUri.ShouldBe(new Uri("https://example.com/api/items?type=all&filter=active"));
    }

    [Fact]
    public async Task SendAsync_WithoutRelativeUrl_UsesBaseUrlDirectly()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var handler = new TestHttpMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            };
        });

        var httpClient = new HttpClient(handler);
        var client = new RestHttpClient(httpClient, logger);

        client.WithBaseUrl("https://example.com/api")
            .WithMediaType("application/json");

        // Act
        await client.SendAsync(HttpMethod.Get, null, null, TestContext.Current.CancellationToken);

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.RequestUri.ShouldBe(new Uri("https://example.com/api"));
    }

    [Fact]
    public async Task SendAsync_WhenResponseIsNoContent_ReturnsNoContentResponse()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var httpClient = new HttpClient(handler);
        var client = new RestHttpClient(httpClient, logger);

        client.WithBaseUrl("https://example.com/api")
            .WithMediaType("application/json");

        // Act
        var response =
            await client.SendAsync(HttpMethod.Delete, "items/1", null, TestContext.Current.CancellationToken);

        // Assert
        response.ShouldNotBeNull();
        response.ShouldSatisfyAllConditions(
            x => x.StatusCode.ShouldBe(HttpStatusCode.NoContent),
            x => x.HasContent.ShouldBeFalse(),
            x => x.Content.ShouldBeNull());
    }

    [Fact]
    public async Task SendAsync_WhenResponseContentIsEmpty_ReturnsHasContentFalse()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler);
        var client = new RestHttpClient(httpClient, logger);

        client.WithBaseUrl("https://example.com/api")
            .WithMediaType("application/json");

        // Act
        var response = await client.SendAsync(HttpMethod.Get, "items", null, TestContext.Current.CancellationToken);

        // Assert
        response.ShouldNotBeNull();
        response.ShouldSatisfyAllConditions(
            x => x.StatusCode.ShouldBe(HttpStatusCode.OK),
            x => x.HasContent.ShouldBeFalse(),
            x => x.Content.ShouldBe(string.Empty));
    }

    [Fact]
    public async Task SendAsync_WhenHttpClientThrowsException_ThrowsRestRequestException()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(_ => throw new HttpRequestException("Network failure"));
        var httpClient = new HttpClient(handler);
        var client = new RestHttpClient(httpClient, logger);

        client.WithBaseUrl("https://example.com/api")
            .WithMediaType("application/json");

        // Act & Assert
        var ex = await Should.ThrowAsync<RestRequestException>(() => client.SendAsync(HttpMethod.Get, "items"));
        ex.Message.ShouldContain("HTTP Request Error: Network failure");
        ex.InnerException.ShouldBeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task SendAsync_WhenServerReturnsErrorStatusCode_ThrowsRestResponseException()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Error details", Encoding.UTF8, "text/plain"),
        });
        var httpClient = new HttpClient(handler);
        var client = new RestHttpClient(httpClient, logger);

        client.WithBaseUrl("https://example.com/api")
            .WithMediaType("application/json");

        // Act & Assert
        var ex = await Should.ThrowAsync<RestResponseException>(() => client.SendAsync(HttpMethod.Get, "items"));
        ex.Message.ShouldContain("HTTP Response Error:");
        ex.InnerException.ShouldBeOfType<HttpRequestException>();
    }
}
