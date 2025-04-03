// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.S3;
using Amazon.S3.Model;
using AWSSDK.Extensions.CrtIntegration;
using Cysharp.IO;
using Elastic.Markdown.IO.State;
using Elastic.Markdown.Links.CrossLinks;

Amazon.RuntimeDependencies.GlobalRuntimeDependencyRegistry.Instance.RegisterChecksumProvider(new CrtChecksums());

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

	IAmazonS3 client = new AmazonS3Client();
	var linkIndex = await CreateLinkIndex(client);
	if (linkIndex == null)
		return $"Error encountered on server. getting list of objects.";

	var json = LinkIndex.Serialize(linkIndex);

	using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
	await client.UploadObjectFromStreamAsync(bucketName, "link-index.json", stream, new Dictionary<string, object>(), CancellationToken.None);
	return $"Finished in {sw}";
}


static async Task<LinkIndex?> CreateLinkIndex(IAmazonS3 client)
{
	var request = new ListObjectsV2Request
	{
		BucketName = bucketName,
		MaxKeys = 5
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
			response = await client.ListObjectsV2Async(request, CancellationToken.None);
			foreach (var obj in response.S3Objects)
			{
				if (!obj.Key.StartsWith("elastic/", StringComparison.OrdinalIgnoreCase))
					continue;

				var tokens = obj.Key.Split('/');
				if (tokens.Length < 3)
					continue;

				// TODO create a dedicated state file for git configuration
				// Deserializing all of the links metadata adds significant overhead
				var gitReference = await ReadLinkReferenceSha(client, bucketName, obj);

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

				Console.WriteLine(entry);
			}

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

static async Task<string> ReadLinkReferenceSha(IAmazonS3 client, string bucketName, S3Object obj)
{
	try
	{
		using var contents = await client.GetObjectAsync(bucketName, obj.Key, CancellationToken.None);
		await using var sr = new Utf8StreamReader(contents.ResponseStream);
		await using var utf8TextReader = new Utf8TextReader(sr);
		var json = await utf8TextReader.ReadToEndAsync();

		var linkReference = LinkReference.Deserialize(json);
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
