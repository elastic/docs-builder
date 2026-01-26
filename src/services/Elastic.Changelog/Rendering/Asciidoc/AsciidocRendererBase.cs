// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using Elastic.Documentation;

namespace Elastic.Changelog.Rendering.Asciidoc;

/// <summary>
/// Abstract base class for asciidoc section renderers with shared utility methods
/// </summary>
public abstract class AsciidocRendererBase
{
	public abstract void Render(IReadOnlyCollection<ChangelogEntry> entries, ChangelogRenderContext context);

	/// <summary>
	/// Gets the entry context (bundleProducts, repo, hideLinks, shouldHide) for a specific entry
	/// </summary>
	private static (HashSet<string> bundleProductIds, string entryRepo, bool hideLinks, bool shouldHide) GetEntryContext(
		ChangelogEntry entry,
		ChangelogRenderContext context)
	{
		var bundleProductIds = context.EntryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
		var entryRepo = context.EntryToRepo.GetValueOrDefault(entry, context.Repo);
		var hideLinks = context.EntryToHideLinks.GetValueOrDefault(entry, false);
		var shouldHide = ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, context);
		return (bundleProductIds, entryRepo, hideLinks, shouldHide);
	}

	/// <summary>
	/// Renders an entry's title and PR/issue links
	/// </summary>
	private static void RenderEntryTitleAndLinks(StringBuilder sb, ChangelogEntry entry, string entryRepo, bool hideLinks, bool shouldHide)
	{
		if (shouldHide)
			_ = sb.AppendLine("// ");

		_ = sb.Append("* ");
		_ = sb.Append(ChangelogTextUtilities.Beautify(entry.Title));

		var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
		var hasIssues = entry.Issues is { Count: > 0 };

		if (!hasPr && !hasIssues)
			return;

		_ = sb.Append(' ');
		if (hasPr)
		{
			_ = sb.Append(ChangelogTextUtilities.FormatPrLinkAsciidoc(entry.Pr!, entryRepo, hideLinks));
			_ = sb.Append(' ');
		}

		if (hasIssues)
		{
			foreach (var issue in entry.Issues!)
			{
				_ = sb.Append(ChangelogTextUtilities.FormatIssueLinkAsciidoc(issue, entryRepo, hideLinks));
				_ = sb.Append(' ');
			}
		}
	}

	/// <summary>
	/// Renders an entry's description with optional comment handling
	/// </summary>
	private static void RenderEntryDescription(StringBuilder sb, ChangelogEntry entry, bool shouldHide)
	{
		if (string.IsNullOrWhiteSpace(entry.Description))
			return;

		_ = sb.AppendLine();
		var indented = ChangelogTextUtilities.Indent(entry.Description);
		if (shouldHide)
		{
			var indentedLines = indented.Split('\n');
			foreach (var line in indentedLines)
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"// {line}");
		}
		else
			_ = sb.AppendLine(indented);
	}

	/// <summary>
	/// Renders Impact and Action fields for breaking changes, deprecations, and known issues
	/// </summary>
	private static void RenderImpactAndAction(StringBuilder sb, ChangelogEntry entry)
	{
		if (!string.IsNullOrWhiteSpace(entry.Impact))
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**Impact:** {entry.Impact}");
		}

		if (!string.IsNullOrWhiteSpace(entry.Action))
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**Action:** {entry.Action}");
		}
	}

	/// <summary>
	/// Renders a complete entry (title, links, description) without Impact/Action
	/// </summary>
	protected void RenderBasicEntry(StringBuilder sb, ChangelogEntry entry, ChangelogRenderContext context)
	{
		var (_, entryRepo, hideLinks, shouldHide) = GetEntryContext(entry, context);
		RenderEntryTitleAndLinks(sb, entry, entryRepo, hideLinks, shouldHide);
		RenderEntryDescription(sb, entry, shouldHide);
		_ = sb.AppendLine();
	}

	/// <summary>
	/// Renders a complete entry with Impact/Action fields
	/// </summary>
	protected void RenderEntryWithImpactAction(StringBuilder sb, ChangelogEntry entry, ChangelogRenderContext context)
	{
		var (_, entryRepo, hideLinks, shouldHide) = GetEntryContext(entry, context);
		RenderEntryTitleAndLinks(sb, entry, entryRepo, hideLinks, shouldHide);
		RenderEntryDescription(sb, entry, shouldHide);
		RenderImpactAndAction(sb, entry);
		_ = sb.AppendLine();
	}
}
