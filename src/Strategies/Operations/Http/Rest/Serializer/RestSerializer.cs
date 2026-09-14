// <copyright file="RestSerializer.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Serializer;

using System.Text.Json;

public static class RestSerializer
{
    private const string FailedToSerializeToJson = "Failed to serialize object to JSON";
    private const string FailedToDeserializeJsonToObject = "Failed to deserialize JSON to object";

    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Serialize<T>(T @object, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(@object);

        try
        {
            return JsonSerializer.Serialize(@object, options ?? DefaultOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(FailedToSerializeToJson, ex);
        }
    }

    public static T Deserialize<T>(string? json, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException(FailedToDeserializeJsonToObject);
        }

        try
        {
            var result = JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);

            return result ?? throw new InvalidOperationException(FailedToDeserializeJsonToObject);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(FailedToDeserializeJsonToObject, ex);
        }
    }
}
