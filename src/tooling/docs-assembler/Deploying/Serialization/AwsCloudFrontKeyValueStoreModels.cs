// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Documentation.Assembler.Deploying.Serialization;

public record DescribeKeyValueStoreResponse([property: JsonPropertyName("ETag")] string ETag, [property: JsonPropertyName("KeyValueStore")] KeyValueStore KeyValueStore);
public record KeyValueStore([property: JsonPropertyName("ARN")] string ARN);

public record ListKeysResponse([property: JsonPropertyName("NextToken")] string? NextToken, [property: JsonPropertyName("Items")] List<KeyItem> Items);
public record KeyItem([property: JsonPropertyName("Key")] string Key);

public record UpdateKeysResponse([property: JsonPropertyName("ETag")] string ETag);

public record PutKeyRequestListItem
{
	[JsonPropertyName("Key")]
	public required string Key { get; init; }
	[JsonPropertyName("Value")]
	public required string Value { get; init; }
}

public record DeleteKeyRequestListItem
{
	[JsonPropertyName("Key")]
	public required string Key { get; init; }
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(DescribeKeyValueStoreResponse))]
[JsonSerializable(typeof(ListKeysResponse))]
[JsonSerializable(typeof(UpdateKeysResponse))]
[JsonSerializable(typeof(List<PutKeyRequestListItem>))]
[JsonSerializable(typeof(List<DeleteKeyRequestListItem>))]
internal sealed partial class AwsCloudFrontKeyValueStoreJsonContext : JsonSerializerContext;
