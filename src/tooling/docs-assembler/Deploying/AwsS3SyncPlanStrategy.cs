// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Deploying;

public interface IS3EtagCalculator
{
	Task<string> CalculateS3ETag(string filePath, Cancel ctx = default);
}

public class S3EtagCalculator(ILoggerFactory logFactory, IFileSystem readFileSystem) : IS3EtagCalculator
{
	private readonly ILogger<AwsS3SyncPlanStrategy> _logger = logFactory.CreateLogger<AwsS3SyncPlanStrategy>();

	private static readonly ConcurrentDictionary<string, string> EtagCache = new();

	internal const long PartSize = 5 * 1024 * 1024; // 5MB

	[SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
	public async Task<string> CalculateS3ETag(string filePath, Cancel ctx = default)
	{
		if (EtagCache.TryGetValue(filePath, out var cachedEtag))
		{
			_logger.LogDebug("Using cached ETag for {Path}", filePath);
			return cachedEtag;
		}

		var fileInfo = readFileSystem.FileInfo.New(filePath);
		var fileSize = fileInfo.Length;

		// For files under 5MB, use simple MD5 (matching TransferUtility behavior)
		if (fileSize <= PartSize)
		{
			await using var stream = readFileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			var smallBuffer = new byte[fileSize];
			var bytesRead = await stream.ReadAsync(smallBuffer.AsMemory(0, (int)fileSize), ctx);
			var hash = MD5.HashData(smallBuffer.AsSpan(0, bytesRead));
			var etag = Convert.ToHexStringLower(hash);
			EtagCache[filePath] = etag;
			return etag;
		}

		// For files over 5MB, use multipart format with 5MB parts (matching TransferUtility)
		var parts = (int)Math.Ceiling((double)fileSize / PartSize);

		await using var fileStream = readFileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
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

public class AwsS3SyncPlanStrategy(
	ILoggerFactory logFactory,
	IAmazonS3 s3Client,
	string bucketName,
	AssembleContext context,
	IS3EtagCalculator? calculator = null
)
	: IDocsSyncPlanStrategy
{
	private readonly IS3EtagCalculator _s3EtagCalculator = calculator ?? new S3EtagCalculator(logFactory, context.ReadFileSystem);

	private bool IsSymlink(string path)
	{
		var fileInfo = context.ReadFileSystem.FileInfo.New(path);
		return fileInfo.LinkTarget != null;
	}

	public async Task<SyncPlan> Plan(float? deleteThreshold, Cancel ctx = default)
	{
		var (readToCompletion, remoteObjects) = await ListObjects(ctx);
		var localObjects = context.OutputDirectory.GetFiles("*", SearchOption.AllDirectories)
			.Where(f => !IsSymlink(f.FullName))
			.ToArray();
		var deleteRequests = new ConcurrentBag<DeleteRequest>();
		var addRequests = new ConcurrentBag<AddRequest>();
		var updateRequests = new ConcurrentBag<UpdateRequest>();
		var skipRequests = new ConcurrentBag<SkipRequest>();

		await Parallel.ForEachAsync(localObjects, ctx, async (localFile, token) =>
		{
			var relativePath = Path.GetRelativePath(context.OutputDirectory.FullName, localFile.FullName);
			var destinationPath = relativePath.Replace('\\', '/');

			if (remoteObjects.TryGetValue(destinationPath, out var remoteObject))
			{
				// Check if the ETag differs for updates
				var localETag = await _s3EtagCalculator.CalculateS3ETag(localFile.FullName, token);
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
					DestinationPath = destinationPath
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
			RemoteListingCompleted = readToCompletion,
			DeleteThresholdDefault = deleteThreshold,
			TotalRemoteFiles = remoteObjects.Count,
			TotalSourceFiles = localObjects.Length,
			DeleteRequests = deleteRequests.ToList(),
			AddRequests = addRequests.ToList(),
			UpdateRequests = updateRequests.ToList(),
			SkipRequests = skipRequests.ToList(),
			TotalSyncRequests = deleteRequests.Count + addRequests.Count + updateRequests.Count + skipRequests.Count
		};
	}

	private async Task<(bool readToCompletion, Dictionary<string, S3Object> objects)> ListObjects(Cancel ctx = default)
	{
		var listBucketRequest = new ListObjectsV2Request
		{
			BucketName = bucketName,
			MaxKeys = 1000
		};
		var objects = new List<S3Object>();
		var bucketExists = await S3BucketExists(ctx);
		if (!bucketExists)
		{
			context.Collector.EmitGlobalError("Bucket does not exist, cannot list objects");
			return (false, objects.ToDictionary(o => o.Key));
		}

		var readToCompletion = true;
		ListObjectsV2Response response;
		do
		{
			response = await s3Client.ListObjectsV2Async(listBucketRequest, ctx);
			if (response is null or { S3Objects: null })
			{
				if (response?.IsTruncated == true)
				{
					context.Collector.EmitGlobalError("Failed to list objects in S3 to completion");
					readToCompletion = false;
				}
				break;
			}
			objects.AddRange(response.S3Objects);
			listBucketRequest.ContinuationToken = response.NextContinuationToken;
		} while (response.IsTruncated == true);

		return (readToCompletion, objects.ToDictionary(o => o.Key));
	}

	private async Task<bool> S3BucketExists(Cancel ctx)
	{
		//https://docs.aws.amazon.com/code-library/latest/ug/s3_example_s3_Scenario_DoesBucketExist_section.html
		try
		{
			_ = await s3Client.GetBucketAclAsync(new GetBucketAclRequest
			{
				BucketName = bucketName
			}, ctx);
			return true;
		}
		catch
		{
			return false;
		}
	}
}
