// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.SQSEvents;

namespace Elastic.Documentation.Lambda.LinkIndexUploader;

[JsonSerializable(typeof(SQSEvent))]
[JsonSerializable(typeof(S3EventNotification))]
public partial class SQSEventSerializerContext : JsonSerializerContext;

public class S3EventNotification
{
	[JsonPropertyName("Records")]
	public List<S3EventRecord> Records { get; set; } = [];
}

public class S3EventRecord
{
	[JsonPropertyName("s3")]
	public S3Event S3 { get; set; } = new();
}

public class S3Event
{
	[JsonPropertyName("object")]
	public S3Object S3Object { get; set; } = new();

	[JsonPropertyName("bucket")]
	public S3Bucket Bucket { get; set; } = new();
}

public class S3Bucket
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;
}

public class S3Object
{
	[JsonPropertyName("key")]
	public string Key { get; set; } = string.Empty;

	[JsonPropertyName("eTag")]
	public string ETag { get; set; } = string.Empty;
}
