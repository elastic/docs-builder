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
using Amazon.S3.Model;
using Elastic.Documentation.Lambda.LinkIndexUploader;
using Elastic.Markdown.IO.State;
using Elastic.Markdown.Links.CrossLinks;
using S3Object = Elastic.Documentation.Lambda.LinkIndexUploader.S3Object;

const string bucketName = "elastic-docs-link-index";
const string indexFile = "link-index-test.json";

await LambdaBootstrapBuilder.Create<SQSEvent>(Handler, new SourceGeneratorLambdaJsonSerializer<SQSEventSerializerContext>())
	.Build()
	.RunAsync();

#pragma warning disable CS8321 // Local function is declared but never used
static async Task<SQSBatchResponse> Handler(SQSEvent evnt, ILambdaContext context)
#pragma warning restore CS8321 // Local function is declared but never used
{
	var s3Client = new AmazonS3Client();
	var batchItemFailures = new List<SQSBatchResponse.BatchItemFailure>();

	var getObjectRequest = new GetObjectRequest
	{
		BucketName = bucketName,
		Key = indexFile
	};

	var getObjectResponse = await s3Client.GetObjectAsync(getObjectRequest);
	await using var stream = getObjectResponse.ResponseStream;
	var linkIndex = LinkIndex.Deserialize(stream);
	var currentETag = getObjectResponse.ETag;

	foreach (var message in evnt.Records)
	{
		try
		{
			var linkReferences = await ProcessMessageAsync(s3Client, message, context);
			context.Logger.LogInformation($"{linkReferences.Count} link references processed successfully.");

			foreach (var (s3Object, linkReference) in linkReferences)
			{
				var keyTokens = s3Object.Key.Split('/');
				var repository = keyTokens[1];
				var branch = keyTokens[2];

				// TODO: This cannot be used for now because it's wrong if all link references were updated by the
				// https://github.com/elastic/docs-internal-workflows/actions/workflows/update-all-link-reference.yml workflow
				// var repository = linkReference.Origin.RepositoryName;
				// var branch = linkReference.Origin.Branch;

				var entry = new LinkIndexEntry
				{
					Repository = linkReference.Origin.RepositoryName,
					Branch = linkReference.Origin.Branch,
					ETag = s3Object.ETag,
					Path = s3Object.Key,
					GitReference = linkReference.Origin.Ref
				};

				if (linkIndex.Repositories.TryGetValue(repository, out var existingEntry))
				{
					existingEntry[branch] = entry;
					context.Logger.LogInformation($"Updated existing entry for {repository}@{branch}");
				}
				else
				{
					linkIndex.Repositories.Add(repository, new Dictionary<string, LinkIndexEntry>
					{
						{ branch, entry }
					});
					context.Logger.LogInformation($"Added new entry for {repository}@{branch}");
				}
			}
		}
		catch (Exception)
		{
			//Add failed message identifier to the batchItemFailures list
			batchItemFailures.Add(new SQSBatchResponse.BatchItemFailure
			{
				ItemIdentifier = message.MessageId
			});
		}
	}

	var json = LinkIndex.Serialize(linkIndex);

	var putObjectRequest = new PutObjectRequest
	{
		BucketName = bucketName,
		Key = indexFile,
		ContentBody = json,
		ContentType = "application/json",
		IfMatch = currentETag
	};

	try
	{
		_ = await s3Client.PutObjectAsync(putObjectRequest);
		context.Logger.LogInformation($"Successfully updated {bucketName}/{indexFile}");
		return new SQSBatchResponse(batchItemFailures);
	}
	catch (Exception)
	{
		// if we fail to update the object, we need to return all the messages
		return new SQSBatchResponse(evnt.Records.Select(r => new SQSBatchResponse.BatchItemFailure
		{
			ItemIdentifier = r.MessageId
		}).ToList());
	}
}

static async Task<IReadOnlyCollection<(S3Object, LinkReference)>> ProcessMessageAsync(IAmazonS3 s3Client, SQSEvent.SQSMessage message, ILambdaContext context)
{
	if (string.IsNullOrEmpty(message.Body))
		throw new Exception("No Body in SQS Message.");
	context.Logger.LogInformation($"Received message {message.Body}");
	var s3Event = JsonSerializer.Deserialize<S3EventNotification>(message.Body, SQSEventSerializerContext.Default.S3EventNotification);
	if (s3Event?.Records == null || s3Event.Records.Count == 0)
		throw new Exception("Invalid S3 event message format");
	var linkReferences = new ConcurrentBag<(S3Object, LinkReference)>();
	await Parallel.ForEachAsync(s3Event.Records, async (record, ctx) =>
	{
		var s3Bucket = record.S3.Bucket;
		var s3Object = record.S3.S3Object;
		context.Logger.LogInformation($"Get object {s3Object.Key} from bucket {s3Bucket.Name}");
		var getObjectResponse = await s3Client.GetObjectAsync(s3Bucket.Name, s3Object.Key, ctx);
		await using var stream = getObjectResponse.ResponseStream;
		context.Logger.LogInformation($"Deserializing link reference from {s3Object.Key}");
		var linkReference = LinkReference.Deserialize(stream);
		context.Logger.LogInformation($"Link reference deserialized: {linkReference}");
		linkReferences.Add((s3Object, linkReference));
	});
	context.Logger.LogInformation($"Deserialized {linkReferences.Count} link references from S3 event");
	return linkReferences;
}
