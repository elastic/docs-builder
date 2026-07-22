// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Globbing;
using Elastic.Documentation.Integrations.S3;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Deploying.Synchronization;

public class AwsS3SyncPlanStrategy(
	ILoggerFactory logFactory,
	IAmazonS3 s3Client,
	string bucketName,
	IDocsSyncContext context,
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

	private static bool IsExcluded(string destinationPath, Glob[] globs) =>
		globs.Length > 0 && globs.Any(g => g.IsMatch(destinationPath));

	/// <summary>
	/// Normalize an exclude pattern so it behaves like <c>aws s3 sync --exclude</c>, where
	/// <c>*</c> matches path separators (e.g. <c>_preview/*</c> matches
	/// <c>_preview/pr-42/index.html</c>).  A trailing <c>/*</c> is expanded to <c>/**</c> so
	/// DotNet.Glob treats it as a recursive wildcard.
	/// </summary>
	private static string NormalizePattern(string pattern) =>
		pattern.EndsWith("/*", StringComparison.Ordinal) ? string.Concat(pattern.AsSpan(0, pattern.Length - 2), "/**") : pattern;

	public async Task<SyncPlan> Plan(float? deleteThreshold, string[] excludePatterns, Cancel ctx = default)
	{
		var excludeGlobs = excludePatterns.Select(p => Glob.Parse(NormalizePattern(p))).ToArray();

		// Start S3 listing in background while scanning local files concurrently
		var listTask = ListObjects(ctx);
		var localObjects = context.OutputDirectory.GetFiles("*", SearchOption.AllDirectories)
			.Where(f => !IsSymlink(f.FullName))
			.ToArray();
		var (readToCompletion, remoteObjects) = await listTask;
		var deleteRequests = new ConcurrentBag<DeleteRequest>();
		var addRequests = new ConcurrentBag<AddRequest>();
		var updateRequests = new ConcurrentBag<UpdateRequest>();
		var skipRequests = new ConcurrentBag<SkipRequest>();

		await Parallel.ForEachAsync(localObjects, ctx, async (localFile, token) =>
		{
			var relativePath = Path.GetRelativePath(context.OutputDirectory.FullName, localFile.FullName);
			var destinationPath = relativePath.Replace('\\', '/');

			if (IsExcluded(destinationPath, excludeGlobs))
				return;

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

		// Find deletions (files in S3 but not locally), honouring excludes
		foreach (var remoteObject in remoteObjects)
		{
			if (IsExcluded(remoteObject.Key, excludeGlobs))
				continue;
			var localPath = Path.Join(context.OutputDirectory.FullName, remoteObject.Key.Replace('/', Path.DirectorySeparatorChar));
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
			ExcludePatterns = excludePatterns,
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
