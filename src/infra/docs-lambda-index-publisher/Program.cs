// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Documentation.Lambda.LinkIndexUploader;
using Elastic.Markdown.IO.State;
using Elastic.Markdown.Links.CrossLinks;


const string bucketName = "elastic-docs-link-index";

await LambdaBootstrapBuilder.Create<LinkReference>(Handler, new SourceGeneratorLambdaJsonSerializer<LinkReferenceSerializerContext>())
 	.Build()
	.RunAsync();

// Uncomment to test locally without uploading
await CreateLinkIndex(new AmazonS3Client());


#pragma warning disable CS8321 // Local function is declared but never used
static async Task<string> Handler(LinkReference linkReference, ILambdaContext context)
#pragma warning restore CS8321 // Local function is declared but never used
{
	const int maxRetries = 3;
	var retryCount = 0;

	Console.WriteLine($"Event triggered by {linkReference.Origin.RepositoryName} {linkReference.Origin.Ref}");
	var sw = Stopwatch.StartNew();

	while (true)
	{
		IAmazonS3 s3Client = new AmazonS3Client();
		var linkIndex = await CreateLinkIndex(s3Client);
		if (linkIndex == null)
		{
			Console.WriteLine($"Error encountered on server. getting list of objects.");
			return $"Error encountered on server. getting list of objects.";
		}

		var json = LinkIndex.Serialize(linkIndex);

		// First, get the current object's ETag
		var getObjectRequest = new GetObjectRequest
		{
			BucketName = bucketName,
			Key = "link-index-test.json"
		};

		var getObjectResponse = await s3Client.GetObjectAsync(getObjectRequest);
		var currentETag = getObjectResponse.ETag;

		// Then, when updating the object, use the If-Match condition
		var putObjectRequest = new PutObjectRequest
		{
			BucketName = bucketName,
			Key = "link-index-test.json",
			ContentBody = json,
			ContentType = "application/json",
			IfMatch = currentETag
		};

		try
		{
			var putObjectResponse = await s3Client.PutObjectAsync(putObjectRequest);
			Console.WriteLine($"Created link object with ETag {putObjectResponse.ETag}");
			Console.WriteLine($"Link Index: {json}");
			return $"Finished in {sw}";
		}
		catch (AmazonS3Exception ex) when (ex.ErrorCode == "PreconditionFailed")
		{
			retryCount++;
			if (retryCount >= maxRetries)
			{
				Console.WriteLine($"Error: Failed to update after {maxRetries} attempts. Someone else modified the object since we read it. {ex.Message}");
				return $"Error: Failed to update after {maxRetries} attempts. Someone else modified the object since we read it. {ex.Message}";
			}
			// Wait a short time before retrying
			await Task.Delay(TimeSpan.FromSeconds(1));
		}
	}
}

static async Task<LinkIndex?> CreateLinkIndex(IAmazonS3 s3Client)
{
	var request = new ListObjectsV2Request
	{
		BucketName = bucketName,
		MaxKeys = 1000 //default
	};

	var linkIndex = new LinkIndex
	{
		Repositories = []
	};
	try
	{
		ListObjectsV2Response response;
		do
		{
			response = await s3Client.ListObjectsV2Async(request, CancellationToken.None);
			await Parallel.ForEachAsync(response.S3Objects, async (obj, ctx) =>
			{
				if (!obj.Key.StartsWith("elastic/", StringComparison.OrdinalIgnoreCase))
					return;

				var tokens = obj.Key.Split('/');
				if (tokens.Length < 3)
					return;

				// TODO create a dedicated state file for git configuration
				// Deserializing all of the links metadata adds significant overhead
				var gitReference = await ReadLinkReferenceSha(s3Client, obj);

				var repository = tokens[1];
				var branch = tokens[2];

				var entry = new LinkIndexEntry
				{
					Repository = repository,
					Branch = branch,
					ETag = obj.ETag.Trim('"'),
					Path = obj.Key,
					GitReference = gitReference
				};
				if (linkIndex.Repositories.TryGetValue(repository, out var existingEntry))
					existingEntry[branch] = entry;
				else
				{
					linkIndex.Repositories.Add(repository, new Dictionary<string, LinkIndexEntry>
					{
						{ branch, entry }
					});
				}
			});

			// If the response is truncated, set the request ContinuationToken
			// from the NextContinuationToken property of the response.
			request.ContinuationToken = response.NextContinuationToken;
		} while (response.IsTruncated);
	}
	catch
	{
		return null;
	}

	return linkIndex;
}

static async Task<string> ReadLinkReferenceSha(IAmazonS3 client, S3Object obj)
{
	try
	{
		var contents = await client.GetObjectAsync(obj.Key, obj.Key, CancellationToken.None);
		await using var s = contents.ResponseStream;
		var linkReference = LinkReference.Deserialize(s);
		return linkReference.Origin.Ref;
	}
	catch (Exception e)
	{
		Console.WriteLine(e);
		// it's important we don't fail here we need to fallback gracefully from this so we can fix the root cause
		// of why a repository is not reporting its git reference properly
		return "unknown";
	}
}
