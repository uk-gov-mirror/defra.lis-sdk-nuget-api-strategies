// <copyright file="ServiceCollectionExtensionsTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests;

using Defra.Livestock.Sdk.Api.Strategies;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Soap.Client;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Requests.Pagination;
using Defra.Livestock.Sdk.Api.Strategies.Context;
using Defra.Livestock.Sdk.Api.Strategies.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Client;
using Defra.Livestock.Sdk.Api.Strategies.Requests.Pagination;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddStrategyFramework_ShouldRegisterOperatorContextAndValidators()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddStrategyFramework();

        // Assert
        returnedServices.ShouldBe(services);

        var operatorDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IOperatorContext));
        operatorDescriptor.ShouldNotBeNull();
        operatorDescriptor.ImplementationType.ShouldBe(typeof(OperatorContext));
        operatorDescriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);

        var validatorDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IValidator<PagedQuery>));
        validatorDescriptor.ShouldNotBeNull();
        validatorDescriptor.ImplementationType.ShouldBe(typeof(PagedQueryValidator));
    }

    [Fact]
    public void AddStrategyOperatorContext_ShouldRegisterScopedOperatorContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddStrategyOperatorContext();

        // Assert
        returnedServices.ShouldBe(services);

        var operatorDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IOperatorContext));
        operatorDescriptor.ShouldNotBeNull();
        operatorDescriptor.ImplementationType.ShouldBe(typeof(OperatorContext));
        operatorDescriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddStrategyValidators_ShouldRegisterValidatorsFromExecutingAssembly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddStrategyValidators();

        // Assert
        returnedServices.ShouldBe(services);

        var validatorDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IValidator<PagedQuery>));
        validatorDescriptor.ShouldNotBeNull();
        validatorDescriptor.ImplementationType.ShouldBe(typeof(PagedQueryValidator));
    }

    [Fact]
    public void AddRepoStrategyFactory_ShouldRegisterTransientRepoStrategyFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddRepoStrategyFactory<TestService>();

        // Assert
        returnedServices.ShouldBe(services);

        var factoryDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IRepoStrategyFactory<TestService>));
        factoryDescriptor.ShouldNotBeNull();
        factoryDescriptor.ImplementationType.ShouldBe(typeof(RepoStrategyFactory<TestService>));
        factoryDescriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddSoapStrategyFactory_ShouldRegisterHttpClientAndTransientSoapStrategyFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddSoapStrategyFactory<TestService>();

        // Assert
        returnedServices.ShouldBe(services);

        var httpClientDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ISoapHttpClient));
        httpClientDescriptor.ShouldNotBeNull();

        var factoryDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ISoapStrategyFactory<TestService>));
        factoryDescriptor.ShouldNotBeNull();
        factoryDescriptor.ImplementationType.ShouldBe(typeof(SoapStrategyFactory<TestService>));
        factoryDescriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddSoapStrategyFactory_WhenSoapHttpClientAlreadyRegistered_ShouldNotReRegisterHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ISoapHttpClient, SoapHttpClient>();

        // Act
        services.AddSoapStrategyFactory<TestService>();

        // Assert
        var count = services.Count(sd => sd.ServiceType == typeof(ISoapHttpClient));
        count.ShouldBe(1);
    }

    [Fact]
    public void AddRestStrategyFactory_ShouldRegisterHttpClientAndTransientRestStrategyFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returnedServices = services.AddRestStrategyFactory<TestService>();

        // Assert
        returnedServices.ShouldBe(services);

        var httpClientDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IRestHttpClient));
        httpClientDescriptor.ShouldNotBeNull();

        var factoryDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IRestStrategyFactory<TestService>));
        factoryDescriptor.ShouldNotBeNull();
        factoryDescriptor.ImplementationType.ShouldBe(typeof(RestStrategyFactory<TestService>));
        factoryDescriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddRestStrategyFactory_WhenRestHttpClientAlreadyRegistered_ShouldNotReRegisterHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IRestHttpClient, RestHttpClient>();

        // Act
        services.AddRestStrategyFactory<TestService>();

        // Assert
        var count = services.Count(sd => sd.ServiceType == typeof(IRestHttpClient));
        count.ShouldBe(1);
    }
}
