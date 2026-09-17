// <copyright file="TestHttpStrategyFactory.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Factories;

using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Base;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Services;

public sealed class TestHttpStrategyFactory : HttpStrategyFactoryBase<TestService, TestHttpStrategyFactory>
{
    public TestHttpStrategyFactory()
    {
        SetParentFactory(this);
    }

    public Action<string, string?>? GetDefaultVerboseOutputAction() => DefaultVerboseOutputAction;

    public void CallAttachHttpDefaultsToBuilder<TParentBuilder>(
        IHttpStrategy<TestService, TParentBuilder> strategyBuilder)
        where TParentBuilder : class
    {
        AttachHttpDefaultsToBuilder(strategyBuilder);
    }
}
