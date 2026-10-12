// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Amazon.S3.Model;

namespace Elastic.Documentation.Deploying.Synchronization;

/// <summary>
/// Keeps the content-hashed frontend assets of recent builds in the bucket after a deploy stops shipping them.
/// A page that is already open, or served from a cache, still imports the <c>chunk-*-[hash].js</c> files of the
/// build it was rendered with; deleting those on the next deploy leaves it broken. A family (an asset with the hash
/// removed) changes at most once per build, so its newest N stale versions cover the last N builds. A family that
/// leaves the build altogether keeps its last N versions; growth is bounded by the number of families ever shipped.
/// </summary>
public static partial class StaticAssetRetention
{
	public const int Keep = 10;

	/// <summary>Returns the keys among <paramref name="staleObjects"/> to keep: the newest <see cref="Keep"/> per family.</summary>
	public static HashSet<string> Retain(IEnumerable<S3Object> staleObjects) =>
		staleObjects
			.Select(o => (Object: o, Family: FamilyOf(o.Key)))
			.Where(x => x.Family is not null)
			.GroupBy(x => x.Family, StringComparer.Ordinal)
			.SelectMany(family => family.OrderByDescending(x => x.Object.LastModified).Take(Keep))
			.Select(x => x.Object.Key)
			.ToHashSet(StringComparer.Ordinal);

	/// <summary>
	/// The hashed asset family of a key, e.g. <c>docs/_static/chunk-ModalSearch-DqleNXOZ.js</c> is
	/// <c>docs/_static/chunk-ModalSearch.js</c>. <c>null</c> for anything Vite does not name with a hash.
	/// </summary>
	public static string? FamilyOf(string key)
	{
		var match = HashedStaticAsset().Match(key);
		return match.Success ? $"{match.Groups["family"].Value}{match.Groups["ext"].Value}" : null;
	}

	// Vite's naming from vite.config.ts: chunk-[name]-[hash].js|css, asset-[name]-[hash].ext, their .map siblings,
	// and the main-[hash].js entry once it is hashed as well.
	[GeneratedRegex(@"^(?<family>(?:.*/)?_static/(?:main|chunk-.+|asset-.+))-[A-Za-z0-9_-]{8}(?<ext>\.[a-z0-9]+(?:\.map)?)$")]
	private static partial Regex HashedStaticAsset();
}
