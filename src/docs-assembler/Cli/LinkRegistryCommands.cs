// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using ConsoleAppFramework;
using Elastic.Markdown.CrossLinks;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Cli;

internal sealed class LinkRegistryCommands(ILoggerFactory logger)
{
	[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
	private void AssignOutputLogger()
	{
		var log = logger.CreateLogger<Program>();
		ConsoleApp.Log = msg => log.LogInformation(msg);
		ConsoleApp.LogError = msg => log.LogError(msg);
	}

	/// <summary>
	/// Create an index.json file from all discovered links.json files in our S3 bucket
	/// </summary>
	/// <param name="ctx"></param>
	[Command("update")]
	public async Task CreateLinkIndex(Cancel ctx = default)
	{
		AssignOutputLogger();

		IAmazonS3 client = new AmazonS3Client();
		var bucketName = "elastic-docs-link-index";
		var request = new ListObjectsV2Request
		{
			BucketName = bucketName,
			MaxKeys = 5
		};

		Console.WriteLine("--------------------------------------");
		Console.WriteLine($"Listing the contents of {bucketName}:");
		Console.WriteLine("--------------------------------------");


		var linkIndex = new LinkIndex
		{
			Repositories = []
		};
		try
		{
			ListObjectsV2Response response;
			do
			{
				response = await client.ListObjectsV2Async(request, ctx);
				foreach (var obj in response.S3Objects)
				{
					if (!obj.Key.StartsWith("elastic/"))
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
			Console.WriteLine($"Error encountered on server. Message:'{ex.Message}' getting list of objects.");
		}

		var json = LinkIndex.Serialize(linkIndex);
		Console.WriteLine(json);

		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
		await client.UploadObjectFromStreamAsync(bucketName, "link-index.json", stream, new Dictionary<string, object>(), ctx);

		Console.WriteLine("Uploaded latest https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/link-index.json");
	}
}
