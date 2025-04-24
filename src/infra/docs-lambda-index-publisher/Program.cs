// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.S3.Util;
using Elastic.Documentation.Lambda.LinkIndexUploader;
using Elastic.Markdown.IO.State;
using Elastic.Markdown.Links.CrossLinks;

const string bucketName = "elastic-docs-link-index";
const string indexFile = "link-index-test.json";

await LambdaBootstrapBuilder.Create<SQSEvent, SQSBatchResponse>(Handler, new SourceGeneratorLambdaJsonSerializer<SerializerContext>())
	.Build()
	.RunAsync();

return;

// The SQS queue is configured to trigger when elastic/*/*/links.json files are created or updated.
static async Task<SQSBatchResponse> Handler(SQSEvent ev, ILambdaContext context)
{
	var s3Client = new AmazonS3Client();
	var linkIndexProvider = new LinkIndexProvider(s3Client, context.Logger, bucketName, indexFile);
	var batchItemFailures = new List<SQSBatchResponse.BatchItemFailure>();
	foreach (var message in ev.Records)
	{
		try
		{
			var s3RecordLinkReferenceTuples = await GetS3RecordLinkReferenceTuples(s3Client, message, context);
			foreach (var (s3Record, linkReference) in s3RecordLinkReferenceTuples)
			{
				var newEntry = ConvertToLinkIndexEntry(s3Record, linkReference);
				await linkIndexProvider.UpdateLinkIndexEntry(newEntry);
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
		await linkIndexProvider.Save();
		var response = new SQSBatchResponse(batchItemFailures);
		if (batchItemFailures.Count > 0)
			context.Logger.LogInformation("Failed to process {batchItemFailuresCount} of {allMessagesCount} messages. Returning them to the queue.", batchItemFailures.Count, ev.Records.Count);
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
		return response;
	}
}

static LinkIndexEntry ConvertToLinkIndexEntry(S3EventNotification.S3EventNotificationRecord record, LinkReference linkReference)
{
	var s3Object = record.S3.Object;
	var keyTokens = s3Object.Key.Split('/');
	var repository = keyTokens[1];
	var branch = keyTokens[2];
	return new LinkIndexEntry
	{
		Repository = repository,
		Branch = branch,
		ETag = s3Object.ETag,
		Path = s3Object.Key,
		UpdatedAt = record.EventTime,
		GitReference = linkReference.Origin.Ref
	};
}

static async Task<IReadOnlyCollection<(S3EventNotification.S3EventNotificationRecord, LinkReference)>> GetS3RecordLinkReferenceTuples(IAmazonS3 s3Client,
	SQSEvent.SQSMessage message, ILambdaContext context)
{
	var s3Event = S3EventNotification.ParseJson(message.Body);
	var recordLinkReferenceTuples = new ConcurrentBag<(S3EventNotification.S3EventNotificationRecord, LinkReference)>();
	var linkReferenceProvider = new LinkReferenceProvider(s3Client, context.Logger, bucketName);
	await Parallel.ForEachAsync(s3Event.Records, async (record, ctx) =>
	{
		var linkReference = await linkReferenceProvider.GetLinkReference(record.S3.Object.Key, ctx);
		recordLinkReferenceTuples.Add((record, linkReference));
	});
	return recordLinkReferenceTuples;
}
