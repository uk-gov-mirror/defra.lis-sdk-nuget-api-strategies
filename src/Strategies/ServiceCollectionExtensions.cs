// <copyright file="ServiceCollectionExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies;

using System.Reflection;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Context;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Soap.Client;
using Defra.Livestock.Sdk.Api.Strategies.Context;
using Defra.Livestock.Sdk.Api.Strategies.Operations;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Soap.Client;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddStrategyFramework()
        {
            services.AddStrategyOperatorContext();
            services.AddStrategyValidators();

            return services;
        }

        public IServiceCollection AddStrategyOperatorContext()
        {
            services.AddScoped<IOperatorContext, OperatorContext>();

            return services;
        }

        public IServiceCollection AddStrategyValidators()
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }

        public IServiceCollection AddRepoStrategyFactory<TService>()
            where TService : class
        {
            services.AddTransient<IRepoStrategyFactory<TService>, RepoStrategyFactory<TService>>();

            return services;
        }

        public IServiceCollection AddSoapStrategyFactory<TService>()
            where TService : class
        {
            var isSoapHttpClientRegistered = services.Any(sd => sd.ServiceType == typeof(ISoapHttpClient));

            if (!isSoapHttpClientRegistered)
            {
                services.AddHttpClient<ISoapHttpClient, SoapHttpClient>();
            }

            services.AddTransient<ISoapStrategyFactory<TService>, SoapStrategyFactory<TService>>();

            return services;
        }

        public IServiceCollection AddRestStrategyFactory<TService>()
            where TService : class
        {
            var isRestHttpClientRegistered = services.Any(sd => sd.ServiceType == typeof(IRestHttpClient));

            if (!isRestHttpClientRegistered)
            {
                services.AddHttpClient<IRestHttpClient, RestHttpClient>();
            }

            services.AddTransient<IRestStrategyFactory<TService>, RestStrategyFactory<TService>>();

            return services;
        }
    }
}
