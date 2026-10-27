// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using RazorSlices;

namespace Elastic.Documentation.Site.StyleGuide;

public sealed record LayoutSampleModel(LayoutKind Kind, bool Full, Func<string, string> Static);

/// <summary>
/// Small pages of one layout with placeholder slots. The Breakpoints page shows them in frames of different widths, so each one runs
/// the real media queries and shows how the layout breaks.
/// </summary>
public static class LayoutSamples
{
	public const string Route = "/style-guide/layout-sample";

	private const string FullSuffix = "-full";

	private static readonly (string Slug, LayoutKind Kind)[] Kinds =
	[
		("single", LayoutKind.OneColumn),
		("bands", LayoutKind.Bands),
		("two", LayoutKind.TwoColumns),
		("three", LayoutKind.ThreeColumns)
	];

	public static IReadOnlyList<string> Names { get; } = [.. Kinds.SelectMany(k => new[] { k.Slug, k.Slug + FullSuffix })];

	public static string Slug(LayoutKind kind) => Kinds.First(k => k.Kind == kind).Slug;

	/// <summary>Renders a sample as a complete HTML document, or <c>null</c> when the name is unknown.</summary>
	public static async Task<string?> RenderAsync(string name, Func<string, string> staticUrl, Cancel ctx = default)
	{
		var full = name.EndsWith(FullSuffix, StringComparison.Ordinal);
		var slug = full ? name[..^FullSuffix.Length] : name;
		var match = Kinds.FirstOrDefault(k => k.Slug == slug);
		if (match.Slug is null)
			return null;
		return await LayoutSample.Create(new LayoutSampleModel(match.Kind, full, staticUrl)).RenderAsync(cancellationToken: ctx);
	}
}
