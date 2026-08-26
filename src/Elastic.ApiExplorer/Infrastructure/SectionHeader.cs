// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Supplemental;
using Microsoft.AspNetCore.Html;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// A page section heading. <paramref name="Route"/> adds the operation-page section navigation
/// buttons; <paramref name="ContentTypeBadge"/> adds a content-type badge next to the title.
/// </summary>
public record SectionHeader(string Title, string Anchor, string? Route = null, string? ContentTypeBadge = null);

/// <summary>A leftover <c>##</c> section from a supplemental file, pre-rendered for the view.</summary>
public record ApiPostSection(string Heading, string Anchor, HtmlString BodyHtml)
{
	internal static readonly FrozenSet<string> OperationReservedAnchors = FrozenSet.ToFrozenSet(
	[
		"paths",
		"prerequisites",
		"description",
		"query-params",
		"request-body",
		"response",
		"responses",
		"code-examples",
		"request-examples",
		"response-examples",
		"examples-jump-btn"
	], StringComparer.Ordinal);

	internal static IReadOnlyList<ApiPostSection> From(
		ApiRenderContext context,
		IReadOnlyList<ApiSupplementalSection> sections,
		IReadOnlySet<string>? reservedAnchors = null)
	{
		if (sections.Count == 0)
			return [];

		var used = reservedAnchors is null
			? new HashSet<string>(StringComparer.Ordinal)
			: new HashSet<string>(reservedAnchors, StringComparer.Ordinal);
		var result = new List<ApiPostSection>(sections.Count);
		foreach (var s in sections)
		{
			var anchor = UniqueAnchor(AnchorFor(s.Heading), used);
			result.Add(new ApiPostSection(s.Heading, anchor, ApiMarkdown.Render(context, s.Body)));
		}

		return result;
	}

	internal static string AnchorFor(string heading) =>
		heading.Trim().ToLowerInvariant().Replace(' ', '-');

	internal static string UniqueAnchor(string baseAnchor, ISet<string> used)
	{
		if (used.Add(baseAnchor))
			return baseAnchor;

		for (var n = 2; ; n++)
		{
			var candidate = $"{baseAnchor}-{n}";
			if (used.Add(candidate))
				return candidate;
		}
	}
}
