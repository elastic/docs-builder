// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Elastic.Documentation.Lambda.OpenApiIndex;
using Elastic.Documentation.OpenApiIndex;

const string bucketName = "elastic-docs-openapi-specs";

// Built once per execution environment, so credential and endpoint resolution happens during Lambda's
// init phase instead of on every invocation.
var publisher = new VersionIndexPublisher(new AmazonS3Client(), bucketName);

await LambdaBootstrapBuilder.Create<SQSEvent, SQSBatchResponse>(Handler, new SourceGeneratorLambdaJsonSerializer<SerializerContext>())
	.Build()
	.RunAsync();

return;

// The SQS queue is configured to trigger on S3 ObjectCreated/ObjectRemoved events anywhere in the bucket.
// Every invocation rebuilds the whole index rather than inspecting which key triggered it — see
// VersionIndexPublisher for why.
async Task<SQSBatchResponse> Handler(SQSEvent ev, ILambdaContext context)
{
	try
	{
		var ignoredKeys = await publisher.RefreshAsync(CancellationToken.None);
		if (ignoredKeys.Count > 0)
			context.Logger.LogWarning("Ignored {ignoredCount} object key(s) not shaped as org/repo/version/file: {ignoredKeys}", ignoredKeys.Count, string.Join(", ", ignoredKeys));
		context.Logger.LogInformation("Refreshed {bucketName}/{indexKey} from {recordCount} triggering event(s).", bucketName, VersionIndexPublisher.IndexKey, ev.Records.Count);
		return new SQSBatchResponse([]);
	}
	catch (Exception ex)
	{
		// Return every message in the batch to the queue: the rebuild reads the whole bucket, so
		// retrying only some of the batch would just repeat the exact same LIST-and-rebuild anyway.
		context.Logger.LogError(ex, "Failed to refresh {bucketName}/{indexKey}. Returning all {recordCount} message(s) to the queue.", bucketName, VersionIndexPublisher.IndexKey, ev.Records.Count);
		return new SQSBatchResponse(ev.Records.Select(r => new SQSBatchResponse.BatchItemFailure { ItemIdentifier = r.MessageId }).ToList());
	}
}
