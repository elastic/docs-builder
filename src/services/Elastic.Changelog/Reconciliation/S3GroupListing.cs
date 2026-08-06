// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Documentation.Configuration.ReleaseNotes;

namespace Elastic.Changelog.Reconciliation;

/// <summary>
/// The one listing spec every registry operation shares (reconcile, full group heal, verify):
/// a group's objects are the immediate <c>.yaml</c>/<c>.yml</c> children of its prefix. The
/// <c>/</c> delimiter is load-bearing — branches are stored verbatim, so without it
/// <c>changelog/{org}/{repo}/main/</c> would also sweep in the <c>main/feature/…</c> pool —
/// and pagination runs to completion. The manifest itself and any other non-YAML keys are
/// excluded.
/// </summary>
public static class S3GroupListing
{
	/// <summary>Lists the group's immediate YAML children in <paramref name="bucketName"/>.</summary>
	public static async Task<IReadOnlyList<S3Object>> ListImmediateYamlObjectsAsync(
		IAmazonS3 s3Client,
		string bucketName,
		ChangelogScope scope,
		Cancel ctx)
	{
		var request = new ListObjectsV2Request
		{
			BucketName = bucketName,
			Prefix = scope.Prefix,
			Delimiter = "/"
		};

		var files = new List<S3Object>();
		ListObjectsV2Response response;
		do
		{
			response = await s3Client.ListObjectsV2Async(request, ctx);
			foreach (var obj in response.S3Objects ?? [])
			{
				var file = obj.Key[scope.Prefix.Length..];
				if (!IsYamlFileName(file) || string.Equals(file, ChangelogKeys.RegistryFileName, StringComparison.Ordinal))
					continue;
				files.Add(obj);
			}
			request.ContinuationToken = response.NextContinuationToken;
		} while (response.IsTruncated == true);

		return files;
	}

	/// <summary>True for a single-segment file name ending in <c>.yaml</c> or <c>.yml</c>.</summary>
	public static bool IsYamlFileName(string file) =>
		file.Length > 0
		&& (file.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".yml", StringComparison.OrdinalIgnoreCase));
}
