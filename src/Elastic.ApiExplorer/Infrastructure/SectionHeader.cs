// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Supplemental;
using Microsoft.AspNetCore.Html;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// A page section heading. <paramref name="Route"/> adds the operation-page section navigation
/// buttons; <paramref name="ContentTypeBadge"/> adds a content-type badge next to the title.
/// </summary>
public record SectionHeader(string Title, string Anchor, string? Route = null, string? ContentTypeBadge = null);

/// <summary>How a section's one-line name summary is written, so its punctuation says what the names are.</summary>
public enum SummaryStyle
{
	/// <summary>The fields of a JSON object: <c>{ a, b }</c>.</summary>
	Fields,

	/// <summary>The fields of each object in a JSON array: <c>[{ a, b }]</c>.</summary>
	ArrayOfFields,

	/// <summary>Alternatives, as when a body is a union: <c>A | B</c>.</summary>
	Variants,

	/// <summary>Names that are all needed together, such as required privileges: <c>a + b</c>, as the credentials of one requirement are.</summary>
	All,

	/// <summary>Names that are neither an object's fields nor alternatives, such as parameters or the fields of a form: <c>a, b</c>.</summary>
	List
}

/// <summary>Collapsible Parameters / Query parameters heading with a one-line name summary written in <paramref name="Style"/>.</summary>
public record ParamSectionHeader(string Title, string Anchor, IReadOnlyList<string> Names, SummaryStyle Style = SummaryStyle.Fields)
{
	public static HtmlString WrapAttributes() => new("class=\"api-param-section collapsed\" data-param-section");

	public static HtmlString BodyAttributes(string listId) =>
		new($"class=\"api-param-section-body\" id=\"{listId}\" hidden=\"until-found\"");
}

/// <summary>A leftover <c>##</c> section from a supplemental file, pre-rendered for the view.</summary>
public record ApiPostSection(string Heading, string Anchor, HtmlString BodyHtml, string BodyMarkdown)
{
	internal static readonly FrozenSet<string> OperationReservedAnchors = FrozenSet.ToFrozenSet(
		[
			"paths",
			"required-authorization",
			"authentication",
			"description",
			"path-params",
			"query-params",
			"header-params",
			"request-body",
			"response",
			"responses",
			"code-examples",
			"request-examples",
			"response-examples",
			"examples-jump-btn"
		],
		StringComparer.Ordinal
	);

	internal static IReadOnlyList<ApiPostSection> From(ApiRenderContext context, IReadOnlyList<ApiSupplementalSection> sections)
	{
		if (sections.Count == 0)
			return [];

		var anchors = Anchors(sections);
		var result = new List<ApiPostSection>(sections.Count);
		for (var i = 0; i < sections.Count; i++)
		{
			var s = sections[i];
			result.Add(new ApiPostSection(SplitHeading(s.Heading).Title, anchors[i], ApiMarkdown.Render(context, s.Body), s.Body));
		}

		return result;
	}

	/// <summary>The anchor of each section, without rendering the bodies. Group intro headings use it to avoid these anchors.</summary>
	internal static IReadOnlyList<string> Anchors(IReadOnlyList<ApiSupplementalSection> sections)
	{
		var used = OperationReservedAnchors.ToHashSet(StringComparer.Ordinal);
		var anchors = new List<string>(sections.Count);
		foreach (var s in sections)
		{
			var (title, explicitId) = SplitHeading(s.Heading);
			anchors.Add(ResolveAnchor(title, explicitId, used));
		}

		return anchors;
	}

	internal static (string Title, string? ExplicitId) SplitHeading(string heading)
	{
		var trimmed = heading.Trim();
		var start = trimmed.LastIndexOf("{#", StringComparison.Ordinal);
		if (start < 0 || !trimmed.EndsWith('}'))
			return (trimmed, null);

		var id = trimmed[(start + 2)..^1];
		if (id.Length == 0 || id.Contains(' ') || id.Contains('{') || id.Contains('}'))
			return (trimmed, null);

		var title = trimmed[..start].Trim();
		return (title.Length == 0 ? trimmed : title, id);
	}

	internal static string AnchorFor(string heading) => heading.Trim().ToLowerInvariant().Replace(' ', '-');

	internal static string ResolveAnchor(string title, string? explicitId, ISet<string> used) =>
		UniqueAnchor(explicitId ?? AnchorFor(title), used);

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
