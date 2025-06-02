// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Deploying;

public class AwsS3SyncPlanStrategy(IAmazonS3 s3Client, string bucketName, AssembleContext context, ILoggerFactory loggerFactory) : IDocsSyncPlanStrategy
{
	internal const long PartSize = 5 * 1024 * 1024; // 5MB
	private readonly ILogger<AwsS3SyncPlanStrategy> _logger = loggerFactory.CreateLogger<AwsS3SyncPlanStrategy>();
	private static readonly ConcurrentDictionary<string, string> EtagCache = new();

	public async Task<SyncPlan> Plan(Cancel ctx = default)
	{
		var remoteObjects = await ListObjects(ctx);
		var localObjects = context.OutputDirectory.GetFiles("*", SearchOption.AllDirectories);
		var deleteRequests = new ConcurrentBag<DeleteRequest>();
		var addRequests = new ConcurrentBag<AddRequest>();
		var updateRequests = new ConcurrentBag<UpdateRequest>();
		var skipRequests = new ConcurrentBag<SkipRequest>();

		await Parallel.ForEachAsync(localObjects, ctx, async (localFile, token) =>
		{
			var relativePath = Path.GetRelativePath(context.OutputDirectory.FullName, localFile.FullName);

			_logger.LogInformation("Checking {Path}", relativePath);

			if (remoteObjects.TryGetValue(relativePath, out var remoteObject))
			{
				// Check if the ETag differs for updates
				var localETag = await CalculateS3ETag(localFile.FullName, token);
				var remoteETag = remoteObject.ETag.Trim('"'); // Remove quotes from remote ETag
				if (localETag == remoteETag)
				{
					var skipRequest = new SkipRequest
					{
						LocalPath = localFile.FullName,
						DestinationPath = remoteObject.Key
					};
					skipRequests.Add(skipRequest);
				}
				else
				{
					var updateRequest = new UpdateRequest()
					{
						LocalPath = localFile.FullName,
						DestinationPath = remoteObject.Key
					};
					updateRequests.Add(updateRequest);
				}
			}
			else
			{
				var addRequest = new AddRequest
				{
					LocalPath = localFile.FullName,
					DestinationPath = relativePath
				};
				addRequests.Add(addRequest);
			}
		});

		// Find deletions (files in S3 but not locally)
		foreach (var remoteObject in remoteObjects)
		{
			var localPath = Path.Combine(context.OutputDirectory.FullName, remoteObject.Key.Replace('/', Path.DirectorySeparatorChar));
			if (context.ReadFileSystem.File.Exists(localPath))
				continue;
			var deleteRequest = new DeleteRequest
			{
				DestinationPath = remoteObject.Key
			};
			deleteRequests.Add(deleteRequest);
		}

		return new SyncPlan
		{
			DeleteRequests = deleteRequests.ToList(),
			AddRequests = addRequests.ToList(),
			UpdateRequests = updateRequests.ToList(),
			SkipRequests = skipRequests.ToList(),
			Count = deleteRequests.Count + addRequests.Count + updateRequests.Count + skipRequests.Count
		};
	}

	private async Task<Dictionary<string, S3Object>> ListObjects(Cancel ctx = default)
	{
		var listBucketRequest = new ListObjectsV2Request
		{
			BucketName = bucketName,
			MaxKeys = 1000,
		};
		var objects = new List<S3Object>();
		ListObjectsV2Response response;
		do
		{
			response = await s3Client.ListObjectsV2Async(listBucketRequest, ctx);
			objects.AddRange(response.S3Objects);
			listBucketRequest.ContinuationToken = response?.NextContinuationToken;
		} while (response?.IsTruncated == true);

		return objects.ToDictionary(o => o.Key);
	}

	[SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
	private async Task<string> CalculateS3ETag(string filePath, Cancel ctx = default)
	{
		if (EtagCache.TryGetValue(filePath, out var cachedEtag))
		{
			_logger.LogDebug("Using cached ETag for {Path}", filePath);
			return cachedEtag;
		}

		var fileInfo = context.ReadFileSystem.FileInfo.New(filePath);
		var fileSize = fileInfo.Length;

		// For files under 5MB, use simple MD5 (matching TransferUtility behavior)
		if (fileSize <= PartSize)
		{
			await using var stream = context.ReadFileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			var smallBuffer = new byte[fileSize];
			var bytesRead = await stream.ReadAsync(smallBuffer.AsMemory(0, (int)fileSize), ctx);
			var hash = MD5.HashData(smallBuffer.AsSpan(0, bytesRead));
			var etag = Convert.ToHexStringLower(hash);
			EtagCache[filePath] = etag;
			return etag;
		}

		// For files over 5MB, use multipart format with 5MB parts (matching TransferUtility)
		var parts = (int)Math.Ceiling((double)fileSize / PartSize);

		await using var fileStream = context.ReadFileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		var partBuffer = new byte[PartSize];
		var partHashes = new List<byte[]>();

		for (var i = 0; i < parts; i++)
		{
			var bytesRead = await fileStream.ReadAsync(partBuffer.AsMemory(0, partBuffer.Length), ctx);
			var partHash = MD5.HashData(partBuffer.AsSpan(0, bytesRead));
			partHashes.Add(partHash);
		}

		// Concatenate all part hashes
		var concatenatedHashes = partHashes.SelectMany(h => h).ToArray();
		var finalHash = MD5.HashData(concatenatedHashes);

		var multipartEtag = $"{Convert.ToHexStringLower(finalHash)}-{parts}";
		EtagCache[filePath] = multipartEtag;
		return multipartEtag;
	}
}
