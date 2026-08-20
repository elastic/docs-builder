// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Amazon.CloudFront;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.S3.Util;
using Elastic.Documentation.Lambda.OpenApiIndex;
using Elastic.Documentation.OpenApiIndex;

const string bucketName = "elastic-docs-openapi-specs";

var distributionId = Environment.GetEnvironmentVariable("CLOUDFRONT_DISTRIBUTION_ID") ??
	throw new InvalidOperationException("CLOUDFRONT_DISTRIBUTION_ID environment variable is required");

// Built once per execution environment, so credential and endpoint resolution happens during Lambda's
// init phase instead of on every invocation.
var publisher = new VersionIndexPublisher(new AmazonS3Client(), bucketName);
var invalidator = new CloudFrontCacheInvalidator(new AmazonCloudFrontClient(), distributionId);

await LambdaBootstrapBuilder.Create<SQSEvent, SQSBatchResponse>(Handler, new SourceGeneratorLambdaJsonSerializer<SerializerContext>())
	.Build()
	.RunAsync();

return;

// The SQS queue is configured to trigger on S3 ObjectCreated/ObjectRemoved events under elastic/.
// Every invocation rebuilds the whole index rather than merging the event — see VersionIndexPublisher
// for why — then invalidates CloudFront for index.json and the triggering object keys.
async Task<SQSBatchResponse> Handler(SQSEvent ev, ILambdaContext context)
{
	try
	{
		var objectKeys = ExtractObjectKeys(ev);
		var ignoredKeys = await publisher.RefreshAsync(CancellationToken.None).ConfigureAwait(false);
		if (ignoredKeys.Count > 0)
			context.Logger.LogWarning(
				"Ignored {ignoredCount} object key(s) not shaped as org/repo/version/file: {ignoredKeys}",
				ignoredKeys.Count,
				string.Join(", ", ignoredKeys)
			);

		var paths = OpenApiInvalidationPaths.Build(objectKeys);
		await invalidator.InvalidateAsync(paths, context.AwsRequestId, CancellationToken.None).ConfigureAwait(false);

		context.Logger.LogInformation(
			"Refreshed {bucketName}/{indexKey} and invalidated {pathCount} CloudFront path(s) from {recordCount} triggering event(s).",
			bucketName,
			VersionIndexPublisher.IndexKey,
			paths.Count,
			ev.Records.Count
		);
		return new SQSBatchResponse([]);
	}
	catch (Exception ex)
	{
		// Return every message in the batch to the queue: the rebuild reads the whole bucket, so
		// retrying only some of the batch would just repeat the exact same LIST-and-rebuild anyway.
		context.Logger.LogError(
			ex,
			"Failed to refresh {bucketName}/{indexKey}. Returning all {recordCount} message(s) to the queue.",
			bucketName,
			VersionIndexPublisher.IndexKey,
			ev.Records.Count
		);
		return new SQSBatchResponse(
			ev.Records.Select(r => new SQSBatchResponse.BatchItemFailure { ItemIdentifier = r.MessageId }).ToList()
		);
	}
}

static IReadOnlyList<string> ExtractObjectKeys(SQSEvent ev)
{
	var keys = new HashSet<string>(StringComparer.Ordinal);
	foreach (var message in ev.Records)
	{
		var s3Event = S3EventNotification.ParseJson(message.Body);
		foreach (var record in s3Event.Records)
			_ = keys.Add(Uri.UnescapeDataString(record.S3.Object.Key));
	}

	return [.. keys];
}
