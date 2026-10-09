// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.StyleGuide;

public sealed record StyleGuidePage(string Slug, string Title, string Group, string Lead);

public sealed class StyleGuideViewModel
{
	public required StyleGuidePage Page { get; init; }
	public required IReadOnlyList<StyleGuidePage> Pages { get; init; }
	public required Func<string, string> Static { get; init; }
}

/// <summary>The living style guide served at <c>/style-guide</c>. Pages render real design-system markup, so the guide cannot drift from the product.</summary>
public static class StyleGuideCatalog
{
	public const string Route = "/style-guide";

	public static IReadOnlyList<StyleGuidePage> Pages { get; } =
	[
		new(
			"foundations",
			"Foundations",
			"Foundations",
			"Colour roles, type, spacing, radius and elevation. Every value on this page is read live from the design tokens."
		),
		new(
			"themes",
			"Themes",
			"Foundations",
			"How light, dark and custom themes work, and how to write your own by overriding tokens only."
		),
		new("motion", "Motion", "Foundations", "Durations, easing and the reduced-motion policy.")
	];

	public static StyleGuidePage Default => Pages[0];

	public static StyleGuidePage? Find(string? slug) =>
		string.IsNullOrEmpty(slug)
			? Default
			: Pages.FirstOrDefault(p => string.Equals(p.Slug, slug.Trim('/'), StringComparison.OrdinalIgnoreCase));

	/// <summary>Renders a guide page to a complete HTML document, or <c>null</c> when the slug is unknown.</summary>
	public static async Task<string?> RenderAsync(string? slug, Func<string, string> staticUrl, Cancel ctx = default)
	{
		var page = Find(slug);
		if (page is null)
			return null;

		var model = new StyleGuideViewModel { Page = page, Pages = Pages, Static = staticUrl };
		using var writer = new StringWriter();
		switch (page.Slug)
		{
			case "themes":
				await Themes.Create(model).RenderAsync(writer, cancellationToken: ctx);
				break;
			case "motion":
				await Motion.Create(model).RenderAsync(writer, cancellationToken: ctx);
				break;
			default:
				await Foundations.Create(model).RenderAsync(writer, cancellationToken: ctx);
				break;
		}
		return writer.ToString();
	}
}
