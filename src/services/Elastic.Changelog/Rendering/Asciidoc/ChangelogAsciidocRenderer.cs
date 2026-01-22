// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#pragma warning disable IDE0060 // Remove unused parameter

using System.IO.Abstractions;
using System.Text;
using static System.Globalization.CultureInfo;
using static Elastic.Documentation.Changelog.ChangelogEntryType;

namespace Elastic.Changelog.Rendering.Asciidoc;

/// <summary>
/// Renderer for changelog asciidoc output
/// </summary>
public class ChangelogAsciidocRenderer(IFileSystem fileSystem)
{
	public async Task RenderAsciidoc(ChangelogRenderContext context, Cancel ctx)
	{
		var sb = new StringBuilder();

		// Create section renderers with shared StringBuilder
		var entriesByAreaRenderer = new EntriesByAreaAsciidocRenderer(sb);
		var breakingChangesRenderer = new BreakingChangesAsciidocRenderer(sb);
		var deprecationsRenderer = new DeprecationsAsciidocRenderer(sb);
		var knownIssuesRenderer = new KnownIssuesAsciidocRenderer(sb);

		// Add anchor
		_ = sb.AppendLine(InvariantCulture, $"[[release-notes-{context.TitleSlug}]]");
		_ = sb.AppendLine(InvariantCulture, $"== {context.Title}");
		_ = sb.AppendLine();

		// Group entries by type
		var entriesByType = context.EntriesByType;
		var security = entriesByType.GetValueOrDefault(Security, []);
		var bugFixes = entriesByType.GetValueOrDefault(BugFix, []);
		var features = entriesByType.GetValueOrDefault(Feature, []);
		var enhancements = entriesByType.GetValueOrDefault(Enhancement, []);
		var breakingChanges = entriesByType.GetValueOrDefault(BreakingChange, []);
		var deprecations = entriesByType.GetValueOrDefault(Deprecation, []);
		var knownIssues = entriesByType.GetValueOrDefault(KnownIssue, []);
		var docs = entriesByType.GetValueOrDefault(Docs, []);
		var regressions = entriesByType.GetValueOrDefault(Regression, []);
		var other = entriesByType.GetValueOrDefault(Other, []);

		// Render security updates
		if (security.Count > 0)
		{
			RenderSectionHeader(sb, "security-updates", context.TitleSlug, "Security updates");
			entriesByAreaRenderer.Render(security, context);
			_ = sb.AppendLine();
		}

		// Render bug fixes
		if (bugFixes.Count > 0)
		{
			RenderSectionHeader(sb, "bug-fixes", context.TitleSlug, "Bug fixes");
			entriesByAreaRenderer.Render(bugFixes, context);
			_ = sb.AppendLine();
		}

		// Render features and enhancements
		if (features.Count > 0 || enhancements.Count > 0)
		{
			RenderSectionHeader(sb, "features-enhancements", context.TitleSlug, "New features and enhancements");
			var combined = features.Concat(enhancements).ToList();
			entriesByAreaRenderer.Render(combined, context);
			_ = sb.AppendLine();
		}

		// Render breaking changes
		if (breakingChanges.Count > 0)
		{
			RenderSectionHeader(sb, "breaking-changes", context.TitleSlug, "Breaking changes");
			breakingChangesRenderer.Render(breakingChanges, context);
			_ = sb.AppendLine();
		}

		// Render deprecations
		if (deprecations.Count > 0)
		{
			RenderSectionHeader(sb, "deprecations", context.TitleSlug, "Deprecations");
			deprecationsRenderer.Render(deprecations, context);
			_ = sb.AppendLine();
		}

		// Render known issues
		if (knownIssues.Count > 0)
		{
			RenderSectionHeader(sb, "known-issues", context.TitleSlug, "Known issues");
			knownIssuesRenderer.Render(knownIssues, context);
			_ = sb.AppendLine();
		}

		// Render documentation changes
		if (docs.Count > 0)
		{
			RenderSectionHeader(sb, "docs", context.TitleSlug, "Documentation");
			entriesByAreaRenderer.Render(docs, context);
			_ = sb.AppendLine();
		}

		// Render regressions
		if (regressions.Count > 0)
		{
			RenderSectionHeader(sb, "regressions", context.TitleSlug, "Regressions");
			entriesByAreaRenderer.Render(regressions, context);
			_ = sb.AppendLine();
		}

		// Render other changes
		if (other.Count > 0)
		{
			RenderSectionHeader(sb, "other", context.TitleSlug, "Other changes");
			entriesByAreaRenderer.Render(other, context);
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
		_ = sb.AppendLine(InvariantCulture, $"[[{anchorPrefix}-{titleSlug}]]");
		_ = sb.AppendLine("[float]");
		_ = sb.AppendLine(InvariantCulture, $"=== {title}");
		_ = sb.AppendLine();
	}
}
