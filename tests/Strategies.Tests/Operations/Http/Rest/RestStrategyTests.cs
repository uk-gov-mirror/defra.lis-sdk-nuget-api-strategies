// <copyright file="RestStrategyTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Http.Rest;

using System.Net;
using System.Text.Json;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Models;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Validation;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Data;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Data.Repositories;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using TestResult = Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Data.TestResult;

public class RestStrategyTests
{
    private readonly IRestHttpClient restHttpClient = Substitute.For<IRestHttpClient>();
    private readonly ILogger<TestService> logger = Substitute.For<ILogger<TestService>>();
    private readonly IOperatorContext operatorContext = Substitute.For<IOperatorContext>();

    public RestStrategyTests()
    {
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        restHttpClient.WithVerboseOutput(Arg.Any<Action<string, string?>?>()).Returns(restHttpClient);
        restHttpClient.WithBaseUrl(Arg.Any<string>()).Returns(restHttpClient);
        restHttpClient.WithMediaType(Arg.Any<string>()).Returns(restHttpClient);
        restHttpClient.WithHeader(Arg.Any<string>(), Arg.Any<string>()).Returns(restHttpClient);
        restHttpClient.WithQueryParameter(Arg.Any<string>(), Arg.Any<string>()).Returns(restHttpClient);
    }

    [Fact]
    public void FluentSetters_ShouldReturnStrategyInstance()
    {
        // Arrange
        var strategy = new RestStrategy<TestService>(restHttpClient);
        var options = new JsonSerializerOptions();
        var queryParams = new Dictionary<string, string> { { "filter", "active" } };

        // Act & Assert
        strategy.WithResourceUrl("users").ShouldBe(strategy);
        strategy.WithGet().ShouldBe(strategy);
        strategy.WithPost().ShouldBe(strategy);
        strategy.WithPut().ShouldBe(strategy);
        strategy.WithDelete().ShouldBe(strategy);
        strategy.WithPatch().ShouldBe(strategy);
        strategy.WithHead().ShouldBe(strategy);
        strategy.WithOptions().ShouldBe(strategy);
        strategy.WithQueryParameter("page", "1").ShouldBe(strategy);
        strategy.WithQueryParameters(queryParams).ShouldBe(strategy);
        strategy.WithPayload(new TestEntity { Id = "1", Name = "Test" }).ShouldBe(strategy);
        strategy.WithPayload(() => new TestEntity { Id = "1", Name = "Test" }).ShouldBe(strategy);
        strategy.WithJsonSerializerOptions(options).ShouldBe(strategy);
        strategy.WithVerboseOutput((_, _) => { }).ShouldBe(strategy);
    }

    [Theory]
    [InlineData(null, "value")]
    [InlineData("", "value")]
    [InlineData(" ", "value")]
    [InlineData("key", null)]
    [InlineData("key", "")]
    [InlineData("key", " ")]
    public void WithQueryParameter_WithInvalidInputs_ShouldThrowArgumentException(string? name, string? value)
    {
        // Arrange
        var strategy = new RestStrategy<TestService>(restHttpClient);

        // Act & Assert
        Should.Throw<ArgumentException>(() => strategy.WithQueryParameter(name!, value!));
    }

    [Fact]
    public void WithQueryParameters_WithNullDictionary_ShouldThrowArgumentNullException()
    {
        // Arrange
        var strategy = new RestStrategy<TestService>(restHttpClient);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => strategy.WithQueryParameters(null!));
    }

    [Fact]
    public async Task Execute_WithoutLogger_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new RestStrategy<TestService>(restHttpClient)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithApiDescription("API")
            .WithActionDescription("Action")
            .WithBaseUrl("https://example.com")
            .WithGet();

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute<TestEntity>());
        ex.Message.ShouldBe(StrategyConstants.Errors.LoggerRequired);
    }

    [Fact]
    public async Task Execute_WithoutCancellationToken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new RestStrategy<TestService>(restHttpClient)
            .WithLogger(logger)
            .WithApiDescription("API")
            .WithActionDescription("Action")
            .WithBaseUrl("https://example.com")
            .WithGet();

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute<TestEntity>());
        ex.Message.ShouldBe(StrategyConstants.Errors.CancellationTokenRequired);
    }

    [Fact]
    public async Task Execute_WithoutApiDescription_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new RestStrategy<TestService>(restHttpClient)
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithActionDescription("Action")
            .WithBaseUrl("https://example.com")
            .WithGet();

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute<TestEntity>());
        ex.Message.ShouldBe(HttpStrategyConstants.Errors.ApiDescriptionRequired);
    }

    [Fact]
    public async Task Execute_WithoutActionDescription_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new RestStrategy<TestService>(restHttpClient)
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithApiDescription("API")
            .WithBaseUrl("https://example.com")
            .WithGet();

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute<TestEntity>());
        ex.Message.ShouldBe(StrategyConstants.Errors.ActionDescriptionRequired);
    }

    [Fact]
    public async Task Execute_WithoutBaseUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new RestStrategy<TestService>(restHttpClient)
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithApiDescription("API")
            .WithActionDescription("Action")
            .WithGet();

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute<TestEntity>());
        ex.Message.ShouldBe(HttpStrategyConstants.Errors.BaseUrlRequired);
    }

    [Fact]
    public async Task Execute_WithoutHttpMethod_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new RestStrategy<TestService>(restHttpClient)
            .WithLogger(logger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithApiDescription("API")
            .WithActionDescription("Action")
            .WithBaseUrl("https://example.com");

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute<TestEntity>());
        ex.Message.ShouldBe(RestStrategyConstants.Errors.HttpMethodRequired);
    }

    [Fact]
    public async Task Execute_WithStringReturnType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = ConfigureValidStrategy(restHttpClient, logger);

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute<string>());
        ex.Message.ShouldBe("Cannot deserialize to String");
    }

    [Fact]
    public async Task ExecuteAndTransform_WithStringResponseType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = ConfigureValidStrategy(restHttpClient, logger);

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            strategy.ExecuteAndTransform<string, TestResult>(_ => new TestResult()));
        ex.Message.ShouldBe("Cannot deserialize to String");
    }

    [Fact]
    public async Task Execute_WhenOperatorUnauthorized_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        operatorContext.HasOperator.Returns(true);
        operatorContext.HasAuthenticatedOperator.Returns(false);
        operatorContext.Operator.Returns(new Operator("op-1", false));

        var strategy = ConfigureValidStrategy(restHttpClient, logger)
            .WithOperatorContext(operatorContext)
            .WithRequiresAuthenticatedOperator();

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(() => strategy.Execute<TestEntity>());
    }

    [Fact]
    public async Task Execute_WhenOperatorContextMissingButRequired_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = ConfigureValidStrategy(restHttpClient, logger)
            .WithRequiresAuthenticatedOperator();

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => strategy.Execute<TestEntity>());
        ex.Message.ShouldBe(StrategyConstants.Errors.OperatorContextRequired);
    }

    [Fact]
    public async Task Execute_WhenRequestValidationFails_ShouldThrowRequestValidationException()
    {
        // Arrange
        var strategy = ConfigureValidStrategy(restHttpClient, logger)
            .WithRequestValidation(() =>
                Task.FromResult(
                    new RequestValidationResult([new RequestValidationFailure("Id", "Id is required")])));

        // Act & Assert
        var ex = await Should.ThrowAsync<RequestValidationException>(() => strategy.Execute<TestEntity>());
        ex.Errors.Count().ShouldBe(1);

        logger.ShouldHaveReceived(LogLevel.Warning, "Execute get user [user api] failed validation");
    }

    [Fact]
    public async Task Execute_LifecycleActions_ShouldExecuteInCorrectOrder()
    {
        // Arrange
        var executionOrder = new List<string>();

        var strategy = ConfigureValidStrategy(restHttpClient, logger)
            .WithBeforeExecute(() =>
            {
                executionOrder.Add("BeforeExecute");
                return Task.CompletedTask;
            })
            .WithAfterExecute(() =>
            {
                executionOrder.Add("AfterExecute");
                return Task.CompletedTask;
            });

        restHttpClient.SendAsync(
                HttpMethod.Get,
                "users/1",
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RestResponse
            {
                StatusCode = HttpStatusCode.OK,
                HasContent = true,
                Content = "{\"id\":\"1\",\"name\":\"John\"}",
            }));

        // Act
        var result = await strategy.Execute<TestEntity>();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldSatisfyAllConditions(
            x => x.Id.ShouldBe("1"),
            x => x.Name.ShouldBe("John"));

        executionOrder.ShouldBe(["BeforeExecute", "AfterExecute"]);

        logger.ShouldHaveReceived(LogLevel.Information, "Executing get user [user api] by operator ");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed get user [user api] by operator ");
    }

    [Fact]
    public async Task Execute_WithObjectPayload_SerializesAndSendsPayload()
    {
        // Arrange
        var payload = new TestEntity { Id = "100", Name = "NewUser" };

        var strategy = ConfigureValidStrategy(restHttpClient, logger)
            .WithPost()
            .WithHeader("X-Header", "HeaderValue")
            .WithQueryParameter("version", "1")
            .WithPayload(payload);

        restHttpClient.SendAsync(
                HttpMethod.Post,
                "users/1",
                "{\"id\":\"100\",\"name\":\"NewUser\"}",
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RestResponse
            {
                StatusCode = HttpStatusCode.Created,
                HasContent = true,
                Content = "{\"id\":\"100\",\"name\":\"NewUser\"}",
            }));

        // Act
        var result = await strategy.Execute<TestEntity>();

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe("100");

        restHttpClient.Received(1).WithHeader("X-Header", "HeaderValue");
        restHttpClient.Received(1).WithQueryParameter("version", "1");
    }

    [Fact]
    public async Task Execute_WithPayloadFactory_SerializesAndSendsPayload()
    {
        // Arrange
        var strategy = ConfigureValidStrategy(restHttpClient, logger)
            .WithPut()
            .WithPayload(() => new TestEntity { Id = "200", Name = "UpdatedUser" });

        restHttpClient.SendAsync(
                HttpMethod.Put,
                "users/1",
                "{\"id\":\"200\",\"name\":\"UpdatedUser\"}",
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RestResponse
            {
                StatusCode = HttpStatusCode.OK,
                HasContent = true,
                Content = "{\"id\":\"200\",\"name\":\"UpdatedUser\"}",
            }));

        // Act
        var result = await strategy.Execute<TestEntity>();

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe("200");
    }

    [Fact]
    public async Task Execute_WhenResponseContentMissingAndRequired_ThrowsRestResponseException()
    {
        // Arrange
        var strategy = ConfigureValidStrategy(restHttpClient, logger);

        restHttpClient.SendAsync(
                HttpMethod.Get,
                "users/1",
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RestResponse
            {
                StatusCode = HttpStatusCode.OK,
                HasContent = false,
                Content = null,
            }));

        // Act & Assert
        var ex = await Should.ThrowAsync<RestResponseException>(() => strategy.Execute<TestEntity>());
        ex.Message.ShouldBe(HttpStrategyConstants.Errors.ResponseContentRequired);
    }

    [Fact]
    public async Task ExecuteAndTransform_TransformsResponseObject()
    {
        // Arrange
        var strategy = ConfigureValidStrategy(restHttpClient, logger);

        restHttpClient.SendAsync(
                HttpMethod.Get,
                "users/1",
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RestResponse
            {
                StatusCode = HttpStatusCode.OK,
                HasContent = true,
                Content = "{\"id\":\"1\",\"name\":\"John\"}",
            }));

        // Act
        var result = await strategy.ExecuteAndTransform<TestEntity, TestResult>(e => new TestResult
        {
            MappedName = e.Name,
        });

        // Assert
        result.ShouldNotBeNull();
        result.MappedName.ShouldBe("John");
    }

    [Fact]
    public async Task ExecuteWithoutResponse_ExecutesSuccessfully()
    {
        // Arrange
        var strategy = ConfigureValidStrategy(restHttpClient, logger)
            .WithDelete();

        restHttpClient.SendAsync(
                HttpMethod.Delete,
                "users/1",
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RestResponse
            {
                StatusCode = HttpStatusCode.NoContent,
                HasContent = false,
                Content = null,
            }));

        // Act
        await strategy.ExecuteWithoutResponse();

        // Assert
        logger.ShouldHaveReceived(LogLevel.Information, "Executing get user [user api] by operator ");
        logger.ShouldHaveReceived(LogLevel.Information, "Successfully executed get user [user api] by operator ");
    }

    private static IRestStrategy<TestService> ConfigureValidStrategy(
        IRestHttpClient client,
        ILogger<TestService> testLogger)
    {
        return new RestStrategy<TestService>(client)
            .WithLogger(testLogger)
            .WithCancellationToken(TestContext.Current.CancellationToken)
            .WithApiDescription("User API")
            .WithActionDescription("Get User")
            .WithBaseUrl("https://example.com/api")
            .WithResourceUrl("users/1")
            .WithGet();
    }
}
