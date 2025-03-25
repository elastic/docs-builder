// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Markdown.CrossLinks;

await LambdaBootstrapBuilder.Create(Handler)
	.Build()
	.RunAsync();

static async Task<string> Handler(ILambdaContext context)
{
	var sw = Stopwatch.StartNew();
	IAmazonS3 client = new AmazonS3Client();
	var bucketName = "elastic-docs-link-index";
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

				var repository = tokens[1];
				var branch = tokens[2];

				var entry = new LinkIndexEntry
				{
					Repository = repository,
					Branch = branch,
					ETag = obj.ETag.Trim('"'),
					Path = obj.Key
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
	catch (AmazonS3Exception ex)
	{
		return $"Error encountered on server. Message:'{ex.Message}' getting list of objects.";
	}

	var json = LinkIndex.Serialize(linkIndex);

	using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
	await client.UploadObjectFromStreamAsync(bucketName, "link-index.json", stream, new Dictionary<string, object>(), CancellationToken.None);
	return $"Finished in {sw}";
}
