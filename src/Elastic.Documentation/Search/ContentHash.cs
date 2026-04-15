// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Elastic.Documentation.Search;

/// <summary>Creates a short hex hash from one or more string components.</summary>
public static partial class ContentHash
{
	/// <summary>
	/// Concatenates all components, computes SHA-256, and returns the first 16 hex characters (lowercased).
	/// Compatible with <c>HashedBulkUpdate.CreateHash</c>.
	/// </summary>
	public static string Create(params string[] components) =>
		Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("", components))))[..16].ToLowerInvariant();

	/// <summary>
	/// Collapses all whitespace runs to a single space, trims, then hashes.
	/// Ensures that whitespace-only changes do not produce a different hash.
	/// </summary>
	public static string CreateNormalized(string content) =>
		Create(WhitespaceRuns().Replace(content.Trim(), " "));

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRuns();
}
