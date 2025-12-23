// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Elastic.Markdown.Exporters.Elasticsearch.Enrichment;

/// <summary>
/// Generates enrichment keys for AI enrichment cache lookups.
/// The key includes the prompt hash so that prompt changes trigger automatic cache invalidation.
/// </summary>
public static partial class EnrichmentKeyGenerator
{
	/// <summary>
	/// Generates a content-based enrichment key (without prompt hash).
	/// This allows stale enrichments from old prompts to still be applied when the prompt changes.
	/// New enrichments will gradually replace stale ones as they're generated.
	/// </summary>
	public static string Generate(string title, string body)
	{
		var normalized = NormalizeRegex().Replace(title + body, "").ToLowerInvariant();
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
		return Convert.ToHexString(hash).ToLowerInvariant();
	}

	[GeneratedRegex("[^a-zA-Z0-9]")]
	private static partial Regex NormalizeRegex();
}
