// <copyright file="RestSerializerTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Api.Strategies.Tests.Operations.Http.Rest.Serializer;

using System.Text.Json;
using System.Text.Json.Serialization;
using Defra.Livestock.Sdk.Api.Strategies.Operations.Http.Rest.Serializer;
using Defra.Livestock.Sdk.Api.Strategies.Tests.TestFramework.Data.Repositories;
using Shouldly;

public class RestSerializerTests
{
    private class UnserializableObject
    {
        [JsonConverter(typeof(ThrowingConverter))]
        public string? Value { get; set; }
    }

    private class ThrowingConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException("Deserialization failed intentionally");

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
            throw new NotSupportedException("Serialization failed intentionally");
    }

    [Fact]
    public void Serialize_WithValidObjectAndDefaultOptions_ReturnsCamelCaseJson()
    {
        // Arrange
        var entity = new TestEntity { Id = "123", Name = "TestName" };

        // Act
        var json = RestSerializer.Serialize(entity);

        // Assert
        json.ShouldBe("{\"id\":\"123\",\"name\":\"TestName\"}");
    }

    [Fact]
    public void Serialize_WithCustomOptions_AppliesCustomOptions()
    {
        // Arrange
        var entity = new TestEntity { Id = "123", Name = "TestName" };
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

        // Act
        var json = RestSerializer.Serialize(entity, options);

        // Assert
        json.ShouldBe("{\"id\":\"123\",\"name\":\"TestName\"}");
    }

    [Fact]
    public void Serialize_WithNullObject_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() => RestSerializer.Serialize<TestEntity>(null!));
    }

    [Fact]
    public void Serialize_WhenJsonSerializerThrows_ThrowsInvalidOperationException()
    {
        // Arrange
        var obj = new UnserializableObject { Value = "Test" };

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => RestSerializer.Serialize(obj));
        ex.Message.ShouldBe("Failed to serialize object to JSON");
        ex.InnerException.ShouldNotBeNull();
    }

    [Fact]
    public void Deserialize_WithValidJsonAndDefaultOptions_ReturnsObject()
    {
        // Arrange
        var json = "{\"id\":\"123\",\"name\":\"TestName\"}";

        // Act
        var entity = RestSerializer.Deserialize<TestEntity>(json);

        // Assert
        entity.ShouldNotBeNull();
        entity.ShouldSatisfyAllConditions(
            x => x.Id.ShouldBe("123"),
            x => x.Name.ShouldBe("TestName"));
    }

    [Fact]
    public void Deserialize_WithCustomOptions_ReturnsObject()
    {
        // Arrange
        var json = "{\"id\":\"123\",\"name\":\"TestName\"}";
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

        // Act
        var entity = RestSerializer.Deserialize<TestEntity>(json, options);

        // Assert
        entity.ShouldNotBeNull();
        entity.ShouldSatisfyAllConditions(
            x => x.Id.ShouldBe("123"),
            x => x.Name.ShouldBe("TestName"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Deserialize_WithNullOrWhitespaceJson_ThrowsInvalidOperationException(string? json)
    {
        // Arrange & Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => RestSerializer.Deserialize<TestEntity>(json));
        ex.Message.ShouldBe("Failed to deserialize JSON to object");
    }

    [Fact]
    public void Deserialize_WhenJsonRepresentsNullLiteral_ThrowsInvalidOperationException()
    {
        // Arrange
        var json = "null";

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => RestSerializer.Deserialize<TestEntity>(json));
        ex.Message.ShouldBe("Failed to deserialize JSON to object");
    }

    [Fact]
    public void Deserialize_WithInvalidJsonFormat_ThrowsInvalidOperationException()
    {
        // Arrange
        var json = "{ invalid json }";

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => RestSerializer.Deserialize<TestEntity>(json));
        ex.Message.ShouldBe("Failed to deserialize JSON to object");
        ex.InnerException.ShouldNotBeNull();
    }
}
