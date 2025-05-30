// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.S3.Util;
using Elastic.Documentation.Lambda.LinkIndexUploader;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;

const string bucketName = "elastic-docs-link-index";
const string indexFile = "link-index.json";

await LambdaBootstrapBuilder.Create<SQSEvent, SQSBatchResponse>(Handler, new SourceGeneratorLambdaJsonSerializer<SerializerContext>())
	.Build()
	.RunAsync();

return;

// The SQS queue is configured to trigger when elastic/*/*/links.json files are created or updated.
static async Task<SQSBatchResponse> Handler(SQSEvent ev, ILambdaContext context)
{
	var s3Client = new AmazonS3Client();
	ILinkIndexReaderWriter linkIndexReaderWriter = new AwsS3LinkIndexReaderWriter(s3Client, bucketName, indexFile);
	var batchItemFailures = new List<SQSBatchResponse.BatchItemFailure>();

	var linkRegistry = await linkIndexReaderWriter.GetRegistry();

	foreach (var message in ev.Records)
	{
		context.Logger.LogInformation("Processing message {MessageId}", message.MessageId);
		context.Logger.LogInformation("Message body: {MessageBody}", message.Body);
		try
		{
			var s3RecordLinkReferenceTuples = await GetS3RecordLinkReferenceTuples(linkIndexReaderWriter, message);
			foreach (var (s3Record, linkReference) in s3RecordLinkReferenceTuples)
			{
				var newEntry = ConvertToLinkIndexEntry(s3Record, linkReference);
				linkRegistry = linkRegistry.WithLinkRegistryEntry(newEntry);
			}
		}
		catch (Exception e)
		{
			// Add failed message identifier to the batchItemFailures list
			context.Logger.LogWarning(e, "Failed to process message {MessageId}", message.MessageId);
			batchItemFailures.Add(new SQSBatchResponse.BatchItemFailure
			{
				ItemIdentifier = message.MessageId
			});
		}
	}
	try
	{
		await linkIndexReaderWriter.SaveRegistry(linkRegistry);
		var response = new SQSBatchResponse(batchItemFailures);
		if (batchItemFailures.Count > 0)
			context.Logger.LogInformation("Failed to process {batchItemFailuresCount} of {allMessagesCount} messages. Returning them to the queue.", batchItemFailures.Count, ev.Records.Count);
		var jsonStr = JsonSerializer.Serialize(response, SerializerContext.Default.SQSBatchResponse);
		context.Logger.LogInformation(jsonStr);
		return response;
	}
	catch (Exception ex)
	{
		// If we fail to update the link index, we need to return all messages to the queue
		// so that they can be retried later.
		context.Logger.LogError("Failed to update {bucketName}/{indexFile}. Returning all {recordCount} messages to the queue.", bucketName, indexFile, ev.Records.Count);
		context.Logger.LogError(ex, ex.Message);
		var response = new SQSBatchResponse(ev.Records.Select(r => new SQSBatchResponse.BatchItemFailure
		{
			ItemIdentifier = r.MessageId
		}).ToList());
		var jsonStr = JsonSerializer.Serialize(response, SerializerContext.Default.SQSBatchResponse);
		context.Logger.LogInformation(jsonStr);
		return response;
	}
}

static LinkRegistryEntry ConvertToLinkIndexEntry(S3EventNotification.S3EventNotificationRecord record, RepositoryLinks linkReference)
{
	var s3Object = record.S3.Object;
	var keyTokens = s3Object.Key.Split('/');
	var repository = keyTokens[1];
	var branch = keyTokens[2];
	return new LinkRegistryEntry
	{
		Repository = repository,
		Branch = branch,
		ETag = s3Object.ETag,
		Path = s3Object.Key,
		UpdatedAt = record.EventTime,
		GitReference = linkReference.Origin.Ref
	};
}

static async Task<IReadOnlyCollection<(S3EventNotification.S3EventNotificationRecord, RepositoryLinks)>> GetS3RecordLinkReferenceTuples(ILinkIndexReaderWriter linkIndexReaderWriter,
	SQSEvent.SQSMessage message)
{
	var s3Event = S3EventNotification.ParseJson(message.Body);
	var recordLinkReferenceTuples = new ConcurrentBag<(S3EventNotification.S3EventNotificationRecord, RepositoryLinks)>();
	await Parallel.ForEachAsync(s3Event.Records, async (record, ctx) =>
	{
		var linkReference = await linkIndexReaderWriter.GetRepositoryLinks(record.S3.Object.Key, ctx);
		recordLinkReferenceTuples.Add((record, linkReference));
	});
	return recordLinkReferenceTuples;
}
