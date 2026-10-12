// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Amazon.S3.Model;

namespace Elastic.Documentation.Deploying.Synchronization;

/// <summary>
/// Keeps the content-hashed frontend assets of recent builds in the bucket after a deploy stops shipping them.
/// A page that is already open, or served from a cache, still imports its own <c>main-[hash].js</c> and
/// <c>chunk-*-[hash].js</c>; deleting those on the next deploy leaves it without scripts. Every build that changes
/// a chunk also changes the entry, so keeping the newest N per family keeps the complete graphs of the last N builds.
/// </summary>
public static partial class StaticAssetRetention
{
	public const int DefaultKeep = 10;

	/// <summary>Returns the keys among <paramref name="staleObjects"/> to keep: the newest <paramref name="keep"/> per family.</summary>
	public static HashSet<string> Retain(IEnumerable<S3Object> staleObjects, int keep)
	{
		var retained = new HashSet<string>(StringComparer.Ordinal);
		if (keep <= 0)
			return retained;

		var families = new Dictionary<string, List<S3Object>>(StringComparer.Ordinal);
		foreach (var staleObject in staleObjects)
		{
			if (FamilyOf(staleObject.Key) is not { } family)
				continue;
			if (!families.TryGetValue(family, out var members))
				families[family] = members = [];
			members.Add(staleObject);
		}

		foreach (var members in families.Values)
		{
			foreach (var newest in members.OrderByDescending(o => o.LastModified).Take(keep))
				_ = retained.Add(newest.Key);
		}
		return retained;
	}

	/// <summary>
	/// The hashed asset family of a key, e.g. <c>docs/_static/chunk-ModalSearch-DqleNXOZ.js</c> is
	/// <c>docs/_static/chunk-ModalSearch.js</c>. <c>null</c> for anything Vite does not name with a hash.
	/// </summary>
	public static string? FamilyOf(string key)
	{
		var match = HashedStaticAsset().Match(key);
		return match.Success ? $"{match.Groups["family"].Value}{match.Groups["ext"].Value}" : null;
	}

	// Vite's naming from vite.config.ts: main-[hash].js, chunk-[name]-[hash].js|css, asset-[name]-[hash].ext, plus .map siblings.
	[GeneratedRegex(@"^(?<family>(?:.*/)?_static/(?:main|chunk-.+|asset-.+))-[A-Za-z0-9_-]{8}(?<ext>\.[a-z0-9]+(?:\.map)?)$")]
	private static partial Regex HashedStaticAsset();
}
