// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Navigation;

namespace Elastic.ApiExplorer.Landing;

/// <summary>Display form of a tag's external documentation link.</summary>
public record TagExternalDocsDisplay(string Url, bool IsElasticDocs, string LinkText);

public class TagLandingViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiTag Tag { get; init; }

	/// <summary>Built before the slice renders so the view only iterates.</summary>
	public required IReadOnlyList<ApiOverviewRow> OverviewRows { get; init; }

	public string? DescriptionMarkdown
	{
		get
		{
			if (RenderContext.TagSupplemental.TryGetValue(Tag.Name, out var doc))
				return doc.DescriptionOr(Tag.Description);
			return Tag.Description;
		}
	}

	public IReadOnlyList<ApiPostSection> PostSections =>
		RenderContext.TagSupplemental.TryGetValue(Tag.Name, out var doc) ? ApiPostSection.From(RenderContext, doc.PostSections) : [];

	public TagExternalDocsDisplay? ExternalDocsDisplay =>
		Tag.ExternalDocs is null
			? null
			: new TagExternalDocsDisplay(
				Tag.ExternalDocs.Url,
				ApiPropertyTreeBuilder.IsElasticDocsUrl(Tag.ExternalDocs.Url),
				string.IsNullOrWhiteSpace(Tag.ExternalDocs.Description) ? "Documentation" : Tag.ExternalDocs.Description
			);

	/// <inheritdoc />
	protected override string? LayoutPageTitle => Tag.DisplayName;

	protected override string? LayoutPageDescription => DescriptionMarkdown;
}

public sealed record IntroHeading(string Title, string Slug) : INavigationModel;

internal static partial class IntroHeadings
{
	public static IReadOnlyList<IntroHeading> Parse(string? markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
			return [];

		var headings = new List<IntroHeading>();
		var inFence = false;
		var skippedFirstH1 = false;
		foreach (var rawLine in markdown.ReplaceLineEndings("\n").Split('\n'))
		{
			var line = rawLine.Trim();
			if (IsFence(line))
			{
				inFence = !inFence;
				continue;
			}

			if (inFence || !TryAtx(line, out var level, out var title))
				continue;

			// RenderApiDescription drops the first h1, so it is not on the page.
			if (level == 1 && !skippedFirstH1)
			{
				skippedFirstH1 = true;
				continue;
			}

			headings.Add(new IntroHeading(title, Slug(title)));
		}

		return headings;
	}

	private static bool IsFence(string line) =>
		line.StartsWith("```", StringComparison.Ordinal) || line.StartsWith("~~~", StringComparison.Ordinal);

	private static bool TryAtx(string line, out int level, out string title)
	{
		level = 0;
		title = "";
		var match = AtxHeading().Match(line);
		if (!match.Success)
			return false;

		title = match.Groups[2].Value.Replace("`", "", StringComparison.Ordinal).Trim();
		if (title.Length == 0)
			return false;

		level = match.Groups[1].Value.Length;
		return true;
	}

	private static string Slug(string title)
	{
		var decomposed = title.Normalize(NormalizationForm.FormD);
		var slug = new StringBuilder(decomposed.Length);
		var pendingDash = false;
		foreach (var ch in decomposed)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
				continue;

			var lower = char.ToLowerInvariant(ch);
			if (lower is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '.' or '_')
			{
				if (pendingDash && slug.Length > 0)
					_ = slug.Append('-');
				pendingDash = false;
				_ = slug.Append(lower);
			}
			else if (lower is '-' or ' ' or '\t')
				pendingDash = true;
		}

		return slug.ToString();
	}

	[GeneratedRegex(@"^(#{1,6})[ \t]+(.*?)(?:[ \t]+#+)?[ \t]*$")]
	private static partial Regex AtxHeading();
}
