// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Elastic.Markdown.Exporters.Elasticsearch.Enrichment;

/// <summary>
/// Generates content-addressable hashes for AI enrichment cache lookups.
/// </summary>
public static partial class ContentHashGenerator
{
	public static string Generate(string title, string body)
	{
		var normalized = NormalizeRegex().Replace(title + body, "").ToLowerInvariant();
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
		return Convert.ToHexString(hash).ToLowerInvariant();
	}

	[GeneratedRegex("[^a-zA-Z0-9]")]
	private static partial Regex NormalizeRegex();
}
