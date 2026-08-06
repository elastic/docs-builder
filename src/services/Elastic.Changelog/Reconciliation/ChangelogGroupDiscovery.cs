// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Documentation.Configuration.ReleaseNotes;

namespace Elastic.Changelog.Reconciliation;

/// <summary>
/// Enumerates every registry group present in a bucket by walking the <c>bundle/</c> and
/// <c>changelog/</c> prefixes and deriving each key's scope. Registry keys count too, so a group
/// that only has an orphaned manifest left is still planned (its reconcile deletes the manifest).
/// The reconcile/verify planners union this across both buckets so orphan public groups are
/// covered as well.
/// </summary>
public static class ChangelogGroupDiscovery
{
	/// <summary>Every scope with at least one key in <paramref name="bucketName"/>, keyed by prefix.</summary>
	public static async Task<IReadOnlyDictionary<string, ChangelogScope>> DiscoverGroupsAsync(
		IAmazonS3 s3Client,
		string bucketName,
		Cancel ctx)
	{
		var scopes = new Dictionary<string, ChangelogScope>(StringComparer.Ordinal);
		foreach (var prefix in new[] { ChangelogKeys.BundlePrefix, ChangelogKeys.ChangelogPrefix })
		{
			var request = new ListObjectsV2Request
			{
				BucketName = bucketName,
				Prefix = prefix
			};

			ListObjectsV2Response response;
			do
			{
				response = await s3Client.ListObjectsV2Async(request, ctx);
				foreach (var obj in response.S3Objects ?? [])
				{
					if (ChangelogScope.TryFromKey(obj.Key, out var scope))
						_ = scopes.TryAdd(scope.Prefix, scope);
				}
				request.ContinuationToken = response.NextContinuationToken;
			} while (response.IsTruncated == true);
		}

		return scopes;
	}

	/// <summary>The union of both buckets' groups, ordered by prefix for a stable plan.</summary>
	public static async Task<IReadOnlyList<ChangelogScope>> DiscoverUnionAsync(
		IAmazonS3 s3Client,
		string privateBucketName,
		string publicBucketName,
		Cancel ctx)
	{
		var union = new Dictionary<string, ChangelogScope>(StringComparer.Ordinal);
		foreach (var (prefix, scope) in await DiscoverGroupsAsync(s3Client, privateBucketName, ctx))
			_ = union.TryAdd(prefix, scope);
		foreach (var (prefix, scope) in await DiscoverGroupsAsync(s3Client, publicBucketName, ctx))
			_ = union.TryAdd(prefix, scope);

		return [.. union.Values.OrderBy(s => s.Prefix, StringComparer.Ordinal)];
	}
}
