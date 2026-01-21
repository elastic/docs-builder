// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using Elastic.Changelog.Configuration;
using Elastic.Documentation.Changelog;

namespace Elastic.Changelog.Rendering.Markdown;

/// <summary>
/// Facade renderer for changelog markdown output that delegates to specialized renderers.
/// For new code, prefer using the specialized renderers directly (IndexMarkdownRenderer, BreakingChangesMarkdownRenderer, etc.)
/// </summary>
public class ChangelogMarkdownRenderer(IFileSystem fileSystem)
{
	[Obsolete("Use IndexMarkdownRenderer directly")]
	public async Task RenderIndexMarkdown(
		string outputDir,
		string title,
		string titleSlug,
		string repo,
		IReadOnlyDictionary<string, IReadOnlyCollection<ChangelogData>> entriesByType,
		bool subsections,
		HashSet<string> featureIdsToHide,
		IReadOnlyDictionary<string, RenderBlockersEntry>? renderBlockers,
		Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts,
		Dictionary<ChangelogData, string> entryToRepo,
		Dictionary<ChangelogData, bool> entryToHideLinks,
		Cancel ctx
	)
	{
		var context = CreateContext(outputDir, title, titleSlug, repo, entriesByType, subsections, featureIdsToHide, renderBlockers, entryToBundleProducts, entryToRepo, entryToHideLinks);
		await new IndexMarkdownRenderer(fileSystem).RenderAsync(context, ctx);
	}

	[Obsolete("Use BreakingChangesMarkdownRenderer directly")]
	public async Task RenderBreakingChangesMarkdown(
		string outputDir,
		string title,
		string titleSlug,
		string repo,
		IReadOnlyDictionary<string, IReadOnlyCollection<ChangelogData>> entriesByType,
		bool subsections,
		HashSet<string> featureIdsToHide,
		IReadOnlyDictionary<string, RenderBlockersEntry>? renderBlockers,
		Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts,
		Dictionary<ChangelogData, string> entryToRepo,
		Dictionary<ChangelogData, bool> entryToHideLinks,
		Cancel ctx
	)
	{
		var context = CreateContext(outputDir, title, titleSlug, repo, entriesByType, subsections, featureIdsToHide, renderBlockers, entryToBundleProducts, entryToRepo, entryToHideLinks);
		await new BreakingChangesMarkdownRenderer(fileSystem).RenderAsync(context, ctx);
	}

	[Obsolete("Use DeprecationsMarkdownRenderer directly")]
	public async Task RenderDeprecationsMarkdown(
		string outputDir,
		string title,
		string titleSlug,
		string repo,
		IReadOnlyDictionary<string, IReadOnlyCollection<ChangelogData>> entriesByType,
		bool subsections,
		HashSet<string> featureIdsToHide,
		IReadOnlyDictionary<string, RenderBlockersEntry>? renderBlockers,
		Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts,
		Dictionary<ChangelogData, string> entryToRepo,
		Dictionary<ChangelogData, bool> entryToHideLinks,
		Cancel ctx
	)
	{
		var context = CreateContext(outputDir, title, titleSlug, repo, entriesByType, subsections, featureIdsToHide, renderBlockers, entryToBundleProducts, entryToRepo, entryToHideLinks);
		await new DeprecationsMarkdownRenderer(fileSystem).RenderAsync(context, ctx);
	}

	[Obsolete("Use KnownIssuesMarkdownRenderer directly")]
	public async Task RenderKnownIssuesMarkdown(
		string outputDir,
		string title,
		string titleSlug,
		string repo,
		IReadOnlyDictionary<string, IReadOnlyCollection<ChangelogData>> entriesByType,
		bool subsections,
		HashSet<string> featureIdsToHide,
		IReadOnlyDictionary<string, RenderBlockersEntry>? renderBlockers,
		Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts,
		Dictionary<ChangelogData, string> entryToRepo,
		Dictionary<ChangelogData, bool> entryToHideLinks,
		Cancel ctx
	)
	{
		var context = CreateContext(outputDir, title, titleSlug, repo, entriesByType, subsections, featureIdsToHide, renderBlockers, entryToBundleProducts, entryToRepo, entryToHideLinks);
		await new KnownIssuesMarkdownRenderer(fileSystem).RenderAsync(context, ctx);
	}

	private static ChangelogRenderContext CreateContext(
		string outputDir,
		string title,
		string titleSlug,
		string repo,
		IReadOnlyDictionary<string, IReadOnlyCollection<ChangelogData>> entriesByType,
		bool subsections,
		HashSet<string> featureIdsToHide,
		IReadOnlyDictionary<string, RenderBlockersEntry>? renderBlockers,
		Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts,
		Dictionary<ChangelogData, string> entryToRepo,
		Dictionary<ChangelogData, bool> entryToHideLinks
	) => new()
	{
		OutputDir = outputDir,
		Title = title,
		TitleSlug = titleSlug,
		Repo = repo,
		EntriesByType = entriesByType,
		Subsections = subsections,
		FeatureIdsToHide = featureIdsToHide,
		RenderBlockers = renderBlockers,
		EntryToBundleProducts = entryToBundleProducts,
		EntryToRepo = entryToRepo,
		EntryToHideLinks = entryToHideLinks
	};

	/// <summary>
	/// Determines if an entry should be blocked from rendering based on render blockers configuration.
	/// Use ChangelogRenderUtilities.ShouldBlockEntry instead.
	/// </summary>
	[Obsolete("Use ChangelogRenderUtilities.ShouldBlockEntry instead")]
	public static bool ShouldBlockEntry(ChangelogData entry, HashSet<string> bundleProductIds, IReadOnlyDictionary<string, RenderBlockersEntry>? renderBlockers, out List<string> reasons)
		=> ChangelogRenderUtilities.ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out reasons);
}
