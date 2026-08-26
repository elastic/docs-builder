// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
	internal static IReadOnlyList<ApiPostSection> From(ApiRenderContext context, IReadOnlyList<ApiSupplementalSection> sections)
	{
		if (sections.Count == 0)
			return [];

		return sections.Select(s => new ApiPostSection(
			s.Heading,
			AnchorFor(s.Heading),
			ApiMarkdown.Render(context, s.Body))).ToArray();
	}

	internal static string AnchorFor(string heading) =>
		heading.Trim().ToLowerInvariant().Replace(' ', '-');
}
