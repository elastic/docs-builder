// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Documentation;
using Elastic.Documentation.Links;

const string bucketName = "elastic-docs-link-index";

await LambdaBootstrapBuilder.Create(Handler)
 	.Build()
	.RunAsync();

// Uncomment to test locally without uploading
// await CreateLinkIndex(new AmazonS3Client());

#pragma warning disable CS8321 // Local function is declared but never used
static async Task<string> Handler(ILambdaContext context)
#pragma warning restore CS8321 // Local function is declared but never used
{
	var sw = Stopwatch.StartNew();

	IAmazonS3 s3Client = new AmazonS3Client();
	var linkIndex = await CreateLinkIndex(s3Client, context.Logger);
	if (linkIndex == null)
		return $"Error encountered on server. getting list of objects.";

	var json = LinkIndex.Serialize(linkIndex);

	using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
	await s3Client.UploadObjectFromStreamAsync(bucketName, "link-index.json", stream, new Dictionary<string, object>(), CancellationToken.None);
	return $"Finished in {sw}";
}


static async Task<LinkIndex?> CreateLinkIndex(IAmazonS3 s3Client, ILambdaLogger contextLogger)
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

				contextLogger.LogInformation("");

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
