// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#pragma warning disable IDE0060 // Remove unused parameter

using System.Globalization;
using System.IO.Abstractions;
using System.Text;

namespace Elastic.Changelog.Rendering.Asciidoc;

/// <summary>
/// Renderer for changelog asciidoc output
/// </summary>
public class ChangelogAsciidocRenderer(IFileSystem fileSystem)
{
	private readonly EntriesByAreaAsciidocRenderer _entriesByAreaRenderer = new();
	private readonly BreakingChangesAsciidocRenderer _breakingChangesRenderer = new();
	private readonly DeprecationsAsciidocRenderer _deprecationsRenderer = new();
	private readonly KnownIssuesAsciidocRenderer _knownIssuesRenderer = new();

	public async Task RenderAsciidoc(
		ChangelogRenderContext context,
		List<ChangelogData> entries,
		Cancel ctx
	)
	{
		var sb = new StringBuilder();

		// Add anchor
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[release-notes-{context.TitleSlug}]]");
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"== {context.Title}");
		_ = sb.AppendLine();

		// Group entries by type
		var entriesByType = context.EntriesByType;
		var security = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Security, []);
		var bugFixes = entriesByType.GetValueOrDefault(ChangelogEntryTypes.BugFix, []);
		var features = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Feature, []);
		var enhancements = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Enhancement, []);
		var breakingChanges = entriesByType.GetValueOrDefault(ChangelogEntryTypes.BreakingChange, []);
		var deprecations = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Deprecation, []);
		var knownIssues = entriesByType.GetValueOrDefault(ChangelogEntryTypes.KnownIssue, []);
		var docs = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Docs, []);
		var regressions = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Regression, []);
		var other = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Other, []);

		// Render security updates
		if (security.Count > 0)
		{
			RenderSectionHeader(sb, "security-updates", context.TitleSlug, "Security updates");
			_entriesByAreaRenderer.Render(sb, security, context);
			_ = sb.AppendLine();
		}

		// Render bug fixes
		if (bugFixes.Count > 0)
		{
			RenderSectionHeader(sb, "bug-fixes", context.TitleSlug, "Bug fixes");
			_entriesByAreaRenderer.Render(sb, bugFixes, context);
			_ = sb.AppendLine();
		}

		// Render features and enhancements
		if (features.Count > 0 || enhancements.Count > 0)
		{
			RenderSectionHeader(sb, "features-enhancements", context.TitleSlug, "New features and enhancements");
			var combined = features.Concat(enhancements).ToList();
			_entriesByAreaRenderer.Render(sb, combined, context);
			_ = sb.AppendLine();
		}

		// Render breaking changes
		if (breakingChanges.Count > 0)
		{
			RenderSectionHeader(sb, "breaking-changes", context.TitleSlug, "Breaking changes");
			_breakingChangesRenderer.Render(sb, breakingChanges, context);
			_ = sb.AppendLine();
		}

		// Render deprecations
		if (deprecations.Count > 0)
		{
			RenderSectionHeader(sb, "deprecations", context.TitleSlug, "Deprecations");
			_deprecationsRenderer.Render(sb, deprecations, context);
			_ = sb.AppendLine();
		}

		// Render known issues
		if (knownIssues.Count > 0)
		{
			RenderSectionHeader(sb, "known-issues", context.TitleSlug, "Known issues");
			_knownIssuesRenderer.Render(sb, knownIssues, context);
			_ = sb.AppendLine();
		}

		// Render documentation changes
		if (docs.Count > 0)
		{
			RenderSectionHeader(sb, "docs", context.TitleSlug, "Documentation");
			_entriesByAreaRenderer.Render(sb, docs, context);
			_ = sb.AppendLine();
		}

		// Render regressions
		if (regressions.Count > 0)
		{
			RenderSectionHeader(sb, "regressions", context.TitleSlug, "Regressions");
			_entriesByAreaRenderer.Render(sb, regressions, context);
			_ = sb.AppendLine();
		}

		// Render other changes
		if (other.Count > 0)
		{
			RenderSectionHeader(sb, "other", context.TitleSlug, "Other changes");
			_entriesByAreaRenderer.Render(sb, other, context);
			_ = sb.AppendLine();
		}

		// Write the asciidoc file
		var asciidocPath = fileSystem.Path.Combine(context.OutputDir, $"{context.TitleSlug}.asciidoc");
		var asciidocDir = fileSystem.Path.GetDirectoryName(asciidocPath);
		if (!string.IsNullOrWhiteSpace(asciidocDir) && !fileSystem.Directory.Exists(asciidocDir))
			_ = fileSystem.Directory.CreateDirectory(asciidocDir);

		await fileSystem.File.WriteAllTextAsync(asciidocPath, sb.ToString(), ctx);
	}

	private static void RenderSectionHeader(StringBuilder sb, string anchorPrefix, string titleSlug, string title)
	{
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[{anchorPrefix}-{titleSlug}]]");
		_ = sb.AppendLine("[float]");
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"=== {title}");
		_ = sb.AppendLine();
	}
}
