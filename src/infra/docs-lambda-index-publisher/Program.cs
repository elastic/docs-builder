// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Elastic.Documentation.Lambda.LinkIndexUploader;
using Elastic.Markdown.IO.State;
using Elastic.Markdown.Links.CrossLinks;

// const string bucketName = "elastic-docs-link-index";
// const string indexFile = "link-index-test.json";

await LambdaBootstrapBuilder.Create<SQSEvent>(Handler, new SourceGeneratorLambdaJsonSerializer<SQSEventSerializerContext>())
	.Build()
	.RunAsync();

// Uncomment to test locally without uploading
// await CreateLinkIndex(new AmazonS3Client());

#pragma warning disable CS8321 // Local function is declared but never used
static async Task<SQSBatchResponse> Handler(SQSEvent evnt, ILambdaContext context)
#pragma warning restore CS8321 // Local function is declared but never used
{
	var s3Client = new AmazonS3Client();
	var batchItemFailures = new List<SQSBatchResponse.BatchItemFailure>();

	// var getObjectRequest = new GetObjectRequest
	// {
	// 	BucketName = bucketName,
	// 	Key = indexFile
	// };

	foreach (var message in evnt.Records)
	{
		try
		{
			//process your message
			await ProcessMessageAsync(s3Client, message, context);
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

	return new SQSBatchResponse(batchItemFailures);
	// const int maxRetries = 3;
	// var retryCount = 0;

	// var sw = Stopwatch.StartNew();
	// await Task.Delay(100);
	// foreach (var record in ev.Records)
	// {
	// 	context.Logger.LogInformation($"Received message: {record.Body}");
	//
	// 	// Delete the message from the queue
	// 	var sqsClient = new AmazonSQSClient();
	//
	// }

	// while (true)
	// {
	// 	IAmazonS3 s3Client = new AmazonS3Client();
	// 	var linkIndex = await CreateLinkIndex(s3Client);
	// 	if (linkIndex == null)
	// 	{
	// 		Console.WriteLine($"Error encountered on server. getting list of objects.");
	// 		return $"Error encountered on server. getting list of objects.";
	// 	}

	// 	var json = LinkIndex.Serialize(linkIndex);

	// 	// First, get the current object's ETag
	// 	var getObjectRequest = new GetObjectRequest
	// 	{
	// 		BucketName = bucketName,
	// 		Key = "link-index-test.json"
	// 	};

	// 	var getObjectResponse = await s3Client.GetObjectAsync(getObjectRequest);
	// 	var currentETag = getObjectResponse.ETag;

	// 	// Then, when updating the object, use the If-Match condition
	// 	var putObjectRequest = new PutObjectRequest
	// 	{
	// 		BucketName = bucketName,
	// 		Key = "link-index-test.json",
	// 		ContentBody = json,
	// 		ContentType = "application/json",
	// 		IfMatch = currentETag
	// 	};

	// 	try
	// 	{
	// 		var putObjectResponse = await s3Client.PutObjectAsync(putObjectRequest);
	// 		Console.WriteLine($"Created link object with ETag {putObjectResponse.ETag}");
	// 		Console.WriteLine($"Link Index: {json}");
	// 		return $"Finished in {sw}";
	// 	}
	// 	catch (AmazonS3Exception ex) when (ex.ErrorCode == "PreconditionFailed")
	// 	{
	// 		retryCount++;
	// 		if (retryCount >= maxRetries)
	// 		{
	// 			Console.WriteLine($"Error: Failed to update after {maxRetries} attempts. Someone else modified the object since we read it. {ex.Message}");
	// 			return $"Error: Failed to update after {maxRetries} attempts. Someone else modified the object since we read it. {ex.Message}";
	// 		}
	// 		// Wait a short time before retrying
	// 		await Task.Delay(TimeSpan.FromSeconds(1));
	// 	}
	// }
}

static async Task ProcessMessageAsync(IAmazonS3 s3Client, SQSEvent.SQSMessage message, ILambdaContext context)
{
	if (string.IsNullOrEmpty(message.Body))
		throw new Exception("No Body in SQS Message.");

	context.Logger.LogInformation($"Processed message {message.Body}");

	var s3Event = JsonSerializer.Deserialize<S3EventNotification>(message.Body, SQSEventSerializerContext.Default.S3EventNotification);
	if (s3Event?.Records == null || s3Event.Records.Count == 0)
		throw new Exception("Invalid S3 event message format");

	await Parallel.ForEachAsync(s3Event.Records, async (record, ctx) =>
	{
		var s3Bucket = record.S3.Bucket;
		var s3Object = record.S3.S3Object;
		var getObjectResponse = await s3Client.GetObjectAsync(s3Bucket.Name, s3Object.Key, ctx);
		await using var stream = getObjectResponse.ResponseStream;
		var linkReference = LinkReference.Deserialize(stream);
		context.Logger.LogInformation($"S3 bucket: {s3Event.Records[0].S3.Bucket.Name}");
		context.Logger.LogInformation($"Processing S3 object: {s3Object.Key}");
		context.Logger.LogInformation($"Processed link {linkReference}");
	});

	// TODO: Do interesting work based on the new message
	await Task.CompletedTask;
}

// static async Task<LinkIndex?> CreateLinkIndex(IAmazonS3 s3Client)
// {
// 	var request = new ListObjectsV2Request
// 	{
// 		BucketName = bucketName,
// 		MaxKeys = 1000 //default
// 	};

// 	var linkIndex = new LinkIndex
// 	{
// 		Repositories = []
// 	};
// 	try
// 	{
// 		ListObjectsV2Response response;
// 		do
// 		{
// 			response = await s3Client.ListObjectsV2Async(request, CancellationToken.None);
// 			await Parallel.ForEachAsync(response.S3Objects, async (obj, ctx) =>
// 			{
// 				if (!obj.Key.StartsWith("elastic/", StringComparison.OrdinalIgnoreCase))
// 					return;

// 				var tokens = obj.Key.Split('/');
// 				if (tokens.Length < 3)
// 					return;

// 				// TODO create a dedicated state file for git configuration
// 				// Deserializing all of the links metadata adds significant overhead
// 				var gitReference = await ReadLinkReferenceSha(s3Client, obj);

// 				var repository = tokens[1];
// 				var branch = tokens[2];

// 				var entry = new LinkIndexEntry
// 				{
// 					Repository = repository,
// 					Branch = branch,
// 					ETag = obj.ETag.Trim('"'),
// 					Path = obj.Key,
// 					GitReference = gitReference
// 				};
// 				if (linkIndex.Repositories.TryGetValue(repository, out var existingEntry))
// 					existingEntry[branch] = entry;
// 				else
// 				{
// 					linkIndex.Repositories.Add(repository, new Dictionary<string, LinkIndexEntry>
// 					{
// 						{ branch, entry }
// 					});
// 				}
// 			});

// 			// If the response is truncated, set the request ContinuationToken
// 			// from the NextContinuationToken property of the response.
// 			request.ContinuationToken = response.NextContinuationToken;
// 		} while (response.IsTruncated);
// 	}
// 	catch
// 	{
// 		return null;
// 	}

// 	return linkIndex;
// }

// static async Task<string> ReadLinkReferenceSha(IAmazonS3 client, S3Object obj)
// {
// 	try
// 	{
// 		var contents = await client.GetObjectAsync(obj.Key, obj.Key, CancellationToken.None);
// 		await using var s = contents.ResponseStream;
// 		var linkReference = LinkReference.Deserialize(s);
// 		return linkReference.Origin.Ref;
// 	}
// 	catch (Exception e)
// 	{
// 		Console.WriteLine(e);
// 		// it's important we don't fail here we need to fallback gracefully from this so we can fix the root cause
// 		// of why a repository is not reporting its git reference properly
// 		return "unknown";
// 	}
// }
