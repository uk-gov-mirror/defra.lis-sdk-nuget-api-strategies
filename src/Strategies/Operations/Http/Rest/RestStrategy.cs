// <copyright file="RestStrategy.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest;

using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Operations.Http.Rest.Client;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Base;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Constants;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Serializer;

public sealed class RestStrategy<TService>
    : HttpStrategyBase<TService, IRestStrategy<TService>>,
        IRestStrategy<TService>
    where TService : class
{
    private readonly IRestHttpClient restHttpClient;

    public RestStrategy(IRestHttpClient restHttpClient)
    {
        this.restHttpClient = restHttpClient;

        SetParentBuilder(this);

        WithMediaType(MediaTypeNames.Application.Json);

        WithJsonSerializerOptions(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        });
    }

    private Action<string, string?>? VerboseOutputAction { get; set; }

    private string? ResourceUrl { get; set; }

    private HttpMethod? HttpMethod { get; set; }

    private Func<string?>? PayloadAction { get; set; }

    private Dictionary<string, string> QueryParameters { get; } = new();

    private JsonSerializerOptions? JsonSerializerOptions { get; set; }

    public IRestStrategy<TService> WithResourceUrl(string resourceUrl)
    {
        ResourceUrl = resourceUrl;

        return this;
    }

    public IRestStrategy<TService> WithGet()
    {
        HttpMethod = HttpMethod.Get;

        return this;
    }

    public IRestStrategy<TService> WithPost()
    {
        HttpMethod = HttpMethod.Post;

        return this;
    }

    public IRestStrategy<TService> WithPut()
    {
        HttpMethod = HttpMethod.Put;

        return this;
    }

    public IRestStrategy<TService> WithDelete()
    {
        HttpMethod = HttpMethod.Delete;

        return this;
    }

    public IRestStrategy<TService> WithPatch()
    {
        HttpMethod = HttpMethod.Patch;

        return this;
    }

    public IRestStrategy<TService> WithHead()
    {
        HttpMethod = HttpMethod.Head;

        return this;
    }

    public IRestStrategy<TService> WithOptions()
    {
        HttpMethod = HttpMethod.Options;

        return this;
    }

    public IRestStrategy<TService> WithQueryParameter(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        QueryParameters[name] = value;

        return this;
    }

    public IRestStrategy<TService> WithQueryParameter(Func<bool> expression, string name, string value)
    {
        ArgumentNullException.ThrowIfNull(expression);

        if (expression())
        {
            WithQueryParameter(name, value);
        }

        return this;
    }

    public IRestStrategy<TService> WithQueryParameters(IDictionary<string, string> queryParameters)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        foreach (var (key, value) in queryParameters)
        {
            WithQueryParameter(key, value);
        }

        return this;
    }

    public IRestStrategy<TService> WithPayload<TRequest>(Func<TRequest> payloadAction)
    {
        PayloadAction = () => RestSerializer.Serialize(payloadAction(), JsonSerializerOptions);

        return this;
    }

    public IRestStrategy<TService> WithPayload<TRequest>(TRequest payload)
    {
        PayloadAction = () => RestSerializer.Serialize(payload, JsonSerializerOptions);

        return this;
    }

    public IRestStrategy<TService> WithJsonSerializerOptions(JsonSerializerOptions jsonSerializerOptions)
    {
        JsonSerializerOptions = jsonSerializerOptions;

        return this;
    }

    public IRestStrategy<TService> WithVerboseOutput(Action<string, string?> verboseOutputAction)
    {
        VerboseOutputAction = verboseOutputAction;

        return this;
    }

    public async Task<TResult> Execute<TResult>()
        where TResult : class
    {
        if (typeof(TResult) == typeof(string))
        {
            throw new InvalidOperationException("Cannot deserialize to String");
        }

        var response = await ExecuteExtractResponse(
            transform: content => RestSerializer.Deserialize<TResult>(content, JsonSerializerOptions),
            requiresResponseContent: true);

        return response!;
    }

    public async Task<TResult> ExecuteAndTransform<TResponse, TResult>(Func<TResponse, TResult> transform)
        where TResponse : class
        where TResult : class
    {
        if (typeof(TResponse) == typeof(string))
        {
            throw new InvalidOperationException("Cannot deserialize to String");
        }

        var response = await ExecuteExtractResponse<TResult>(
            transform: content =>
                transform(RestSerializer.Deserialize<TResponse>(content!, JsonSerializerOptions)),
            requiresResponseContent: true);

        return response!;
    }

    public async Task ExecuteWithoutResponse()
    {
        await ExecuteExtractResponse<string>(
            transform: _ => null,
            requiresResponseContent: false);
    }

    [SuppressMessage(
        "SonarAnalyzer.CSharp",
        "S3776: Cognitive Complexity of methods should not be too high",
        Justification = "Reviewed. Due to necessary checks as part of the fluent builder pattern")
    ]
    private async Task<TResult?> ExecuteExtractResponse<TResult>(
        Func<string?, TResult?> transform,
        bool requiresResponseContent = true)
        where TResult : class
    {
        if (Logger == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.LoggerRequired);
        }

        if (CancellationToken == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.CancellationTokenRequired);
        }

        if (TargetDescription == null)
        {
            throw new InvalidOperationException(HttpStrategyConstants.Errors.ApiDescriptionRequired);
        }

        if (ActionDescription == null)
        {
            throw new InvalidOperationException(StrategyConstants.Errors.ActionDescriptionRequired);
        }

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new InvalidOperationException(HttpStrategyConstants.Errors.BaseUrlRequired);
        }

        if (HttpMethod == null)
        {
            throw new InvalidOperationException(RestStrategyConstants.Errors.HttpMethodRequired);
        }

        if (string.IsNullOrWhiteSpace(MediaType))
        {
            throw new InvalidOperationException(HttpStrategyConstants.Errors.MediaTypeRequired);
        }

        EnsureOperatorHasRequiredPermissions();

        LogExecutingAction();

        await InvokeBeforeExecuteAction();

        await ExecuteRequestValidation();

        var payload = PayloadAction?.Invoke();

        restHttpClient
            .WithVerboseOutput(VerboseOutputAction)
            .WithBaseUrl(BaseUrl)
            .WithMediaType(MediaType);

        foreach (var header in Headers)
        {
            restHttpClient.WithHeader(header.Key, header.Value);
        }

        foreach (var queryParameter in QueryParameters)
        {
            restHttpClient.WithQueryParameter(queryParameter.Key, queryParameter.Value);
        }

        var restResponse = await restHttpClient.SendAsync(
            HttpMethod,
            ResourceUrl,
            payload,
            CancellationToken.Value);

        if (requiresResponseContent && !restResponse.HasContent)
        {
            throw new RestResponseException(HttpStrategyConstants.Errors.ResponseContentRequired);
        }

        var content = restResponse.Content;

        var transformedResponse = transform(content);

        await InvokeAfterExecuteAction();

        LogSuccessfullyExecutedAction();

        return transformedResponse;
    }
}
