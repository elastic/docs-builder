// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.RegularExpressions;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// The single source of URL path segments (monikers) for API explorer pages.
/// </summary>
public static partial class ApiUrlBuilder
{
	public static string ApiRoot(string? urlPathPrefix) => $"{urlPathPrefix?.TrimEnd('/')}/api";

	public static string ProductRoot(string? urlPathPrefix, string apiUrlSuffix) => $"{ApiRoot(urlPathPrefix)}/{apiUrlSuffix}";

	/// <summary>
	/// Deterministic URL leaf for an operation page under <c>.../operation/</c>: lowercase
	/// <c>operation-{id}</c> when an operation id is present, otherwise derived from the route.
	/// Dots become hyphens so a trailing <c>.json</c> stays a path segment.
	/// </summary>
	public static string OperationMoniker(string? operationId, string route)
	{
		var id = !string.IsNullOrWhiteSpace(operationId)
			? operationId
			: route.Replace("}", "").Replace("{", "").Replace('/', '-').Trim('-');

		return $"operation-{id.Replace('.', '-').ToLowerInvariant()}";
	}

	/// <summary>
	/// URL leaf for operations collapsed onto one page. Spec operation ids for one API share a base and differ by
	/// a numeric suffix (<c>search</c>, <c>search-1</c>…), so the page keeps the primary operation's base id. The
	/// suffix stays when no other operation in the group shares that base, or when the base moniker is already a
	/// page outside the group (<paramref name="takenMonikers"/>: every operation's own moniker, plus the shared
	/// pages created so far).
	/// </summary>
	public static string CanonicalOperationMoniker(
		IReadOnlyList<ApiOperation> operations,
		ApiOperation primary,
		IReadOnlySet<string>? takenMonikers = null
	)
	{
		var id = primary.Operation.OperationId;
		if (string.IsNullOrWhiteSpace(id))
			return OperationMoniker(id, primary.Route);

		var baseId = NumericSuffix().Replace(id, "");
		var shared = operations.Count(o => o.Operation.OperationId is { } other && NumericSuffix().Replace(other, "") == baseId) > 1;
		if (!shared)
			return OperationMoniker(id, primary.Route);

		var candidate = OperationMoniker(baseId, primary.Route);
		var ownedByGroup = operations.Any(o => OperationMoniker(o.Operation.OperationId, o.Route) == candidate);
		var takenElsewhere = !ownedByGroup && takenMonikers?.Contains(candidate) == true;
		return takenElsewhere ? OperationMoniker(id, primary.Route) : candidate;
	}

	[GeneratedRegex(@"-\d+$", RegexOptions.CultureInvariant, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex NumericSuffix();

	/// <summary>Deterministic URL segment for a schema type page under <c>.../types/</c>.</summary>
	public static string SchemaMoniker(string schemaId) => schemaId.Replace('.', '-').ToLowerInvariant();

	/// <summary>
	/// URL slug for a tag, without the <c>endpoint-</c> prefix. Spaces become hyphens and the
	/// result is lowercased; underscores are kept. Empty or whitespace names become <c>unknown</c>.
	/// </summary>
	public static string TagSlug(string? tagName)
	{
		if (string.IsNullOrWhiteSpace(tagName))
			return "unknown";

		var s = tagName.Trim();
		s = string.Join(" ", s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
		s = ParentheticalSuffixPattern().Replace(s, "-$1");
		s = s.Replace("{", string.Empty, StringComparison.Ordinal);
		s = s.Replace("}", string.Empty, StringComparison.Ordinal);
		s = s.Replace("/", "-", StringComparison.Ordinal);
		s = s.Replace(" ", "-", StringComparison.Ordinal);
		s = s.ToLowerInvariant();
		return string.IsNullOrEmpty(s) ? "unknown" : s;
	}

	/// <summary>Deterministic URL leaf for <c>.../group/{segment}</c> from the canonical tag name.</summary>
	public static string TagMoniker(string? tagName) => $"endpoint-{TagSlug(tagName)}";

	public const string AuthenticationSegment = "authentication";
	public const string ServersSegment = "servers";
	public const string TopicSegment = "topic";

	/// <summary>The URL segment of the unreleased <c>main</c> spec when a released major owns the versionless URL.</summary>
	public const string NextSegment = "next";

	/// <summary>Child markdown slugs that collide with generated API Explorer paths.</summary>
	public static readonly string[] ReservedChildSegments =
	[
		"types",
		"group",
		"operation",
		TopicSegment,
		AuthenticationSegment,
		ServersSegment,
		NextSegment
	];

	/// <summary>URL leaf for an <c>x-topics</c> page under <c>.../topic/</c>, the same as Bump.sh: <c>Rate limiting</c> becomes <c>topic-rate-limiting</c>.</summary>
	public static string TopicMoniker(string title) => $"topic-{TagSlug(title)}";

	public static string TopicUrl(string? urlPathPrefix, string apiUrlSuffix, string title) =>
		$"{ProductRoot(urlPathPrefix, apiUrlSuffix)}/{TopicSegment}/{TopicMoniker(title)}";

	/// <summary>URL of a <c>children:</c> page, from its file slug.</summary>
	public static string ChildUrl(string? urlPathPrefix, string apiUrlSuffix, string slug) =>
		$"{ProductRoot(urlPathPrefix, apiUrlSuffix)}/{slug}";

	/// <summary>
	/// Anchor of one alternative on the Authentication page, the same as Bump.sh: <c>apiKeyAuth</c> becomes
	/// <c>doc-authentication-api-key-auth</c>. Schemes sent together join their ids.
	/// </summary>
	public static string AuthenticationAnchor(IEnumerable<string> schemeIds) =>
		$"doc-authentication-{string.Join("-", schemeIds.Select(KebabCase))}";

	private static string KebabCase(string id)
	{
		var kebab = new StringBuilder(id.Length + 4);
		for (var i = 0; i < id.Length; i++)
		{
			var c = id[i];
			if (char.IsUpper(c) && i > 0 && char.IsLetterOrDigit(id[i - 1]) && !char.IsUpper(id[i - 1]))
				_ = kebab.Append('-');
			_ = kebab.Append(c is '_' or ' ' or '.' ? '-' : char.ToLowerInvariant(c));
		}

		return kebab.ToString();
	}

	public static string AuthenticationUrl(string? urlPathPrefix, string apiUrlSuffix) =>
		$"{ProductRoot(urlPathPrefix, apiUrlSuffix)}/{AuthenticationSegment}";

	public static string ServersUrl(string? urlPathPrefix, string apiUrlSuffix) =>
		$"{ProductRoot(urlPathPrefix, apiUrlSuffix)}/{ServersSegment}";

	public static string OperationUrl(string? urlPathPrefix, string apiUrlSuffix, string moniker) =>
		$"{ProductRoot(urlPathPrefix, apiUrlSuffix)}/operation/{moniker}";

	[GeneratedRegex(@"\s*\(([^)]+)\)", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex ParentheticalSuffixPattern();
}
