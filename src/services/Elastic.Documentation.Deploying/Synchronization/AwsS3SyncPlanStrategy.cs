// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Globbing;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Integrations.S3;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Deploying.Synchronization;

public class AwsS3SyncPlanStrategy(
	ILoggerFactory logFactory,
	IAmazonS3 s3Client,
	string bucketName,
	IDocsSyncContext context,
	IS3EtagCalculator? calculator = null
) : IDocsSyncPlanStrategy
{
	private readonly IS3EtagCalculator _s3EtagCalculator = calculator ?? new S3EtagCalculator(logFactory, context.ReadFileSystem);

	private bool IsSymlink(string path)
	{
		var fileInfo = context.ReadFileSystem.FileInfo.New(path);
		return fileInfo.LinkTarget != null;
	}

	public async Task<Result<SyncPlan>> Plan(float? deleteThreshold, string[] excludePatterns, Cancel ctx = default)
	{
		var excludeGlobs = excludePatterns.Select(p => Glob.Parse(SyncPlanner.NormalizePattern(p))).ToArray();

		// Start S3 listing in background while scanning local files concurrently
		var listTask = ListObjects(ctx);
		var localObjects = context.OutputDirectory.GetFiles("*", SearchOption.AllDirectories).Where(f => !IsSymlink(f.FullName)).ToArray();
		var (readToCompletion, remoteObjects) = await listTask;

		var localFiles = localObjects.Select(
			f =>
				(LocalPath: f.FullName, DestinationPath: Path.GetRelativePath(context.OutputDirectory.FullName, f.FullName).Replace(
					'\\',
					'/'
				))
		).ToArray();
		var remoteETags = remoteObjects.ToDictionary(
			o => o.Key,
			o => o.Value.ETag?.Trim('"') ?? string.Empty
		); // Remove quotes from remote ETag

		// only a file that is also remote needs its ETag (a hash of the file), so the work stays what it was
		var localETags = new ConcurrentDictionary<string, string>();
		await Parallel.ForEachAsync(
			localFiles.Where(f => remoteETags.ContainsKey(f.DestinationPath) && !SyncPlanner.IsExcluded(f.DestinationPath, excludeGlobs)),
			ctx,
			async (f, token) => localETags[f.LocalPath] = await _s3EtagCalculator.CalculateS3ETag(f.LocalPath, token)
		);

		var (deleteRequests, addRequests, updateRequests, skipRequests) = SyncPlanner.Compute(
			localFiles,
			remoteETags,
			localPath => localETags[localPath],
			key => context
				.ReadFileSystem
				.File
				.Exists(Path.Join(context.OutputDirectory.FullName, key.Replace('/', Path.DirectorySeparatorChar))),
			excludeGlobs
		);

		return SyncPlanLoader.Planned(
			readToCompletion,
			deleteThreshold,
			excludePatterns,
			remoteObjects.Count,
			localObjects.Length,
			deleteRequests,
			addRequests,
			updateRequests,
			skipRequests
		);
	}

	private async Task<(bool readToCompletion, Dictionary<string, S3Object> objects)> ListObjects(Cancel ctx = default)
	{
		var listBucketRequest = new ListObjectsV2Request { BucketName = bucketName, MaxKeys = 1000 };
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
		}
		while (response.IsTruncated == true);

		return (readToCompletion, objects.ToDictionary(o => o.Key));
	}

	private async Task<bool> S3BucketExists(Cancel ctx)
	{
		//https://docs.aws.amazon.com/code-library/latest/ug/s3_example_s3_Scenario_DoesBucketExist_section.html
		try
		{
			_ = await s3Client.GetBucketAclAsync(new GetBucketAclRequest { BucketName = bucketName }, ctx);
			return true;
		}
		catch
		{
			return false;
		}
	}
}
