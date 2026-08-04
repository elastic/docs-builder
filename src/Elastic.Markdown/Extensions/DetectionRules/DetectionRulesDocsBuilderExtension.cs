// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Toc.DetectionRules;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Isolated.Node;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst;

namespace Elastic.Markdown.Extensions.DetectionRules;

public class DetectionRulesDocsBuilderExtension(BuildContext build) : IDocsBuilderExtension
{
	private BuildContext Build => build;
	private bool _versionLockInitialized;

	private IReadOnlySet<string> DeprecatedOverviewFileNames { get; } =
		GetAllDetectionRuleOverviewRefs(build.ConfigurationYaml.TableOfContents)
			.Select(r => r.DeprecatedFile ?? "deprecated-detection-rules.md")
			.ToHashSet();

	public IEnumerable<string> ExternalScopeRoots =>
		GetAllDetectionRuleOverviewRefs(Build.ConfigurationYaml.TableOfContents)
			.SelectMany(r => r.DetectionRuleFolders)
			.Select(f => Path.GetFullPath(f, Build.DocumentationSourceDirectory.FullName))
			.Distinct();

	public IDocumentationFileExporter? FileExporter { get; } = new RuleDocumentationFileExporter(build.ReadFileSystem, build.WriteFileSystem);

	public DocumentationFile? CreateDocumentationFile(IFileInfo file, MarkdownParser markdownParser)
	{
		// Handle the synthetic deprecated overview .md file when no physical file exists on disk
		if (file.Extension == ".md" && DeprecatedOverviewFileNames.Contains(file.Name))
			return new DeprecatedDetectionRuleOverviewFile(file, Build.DocumentationSourceDirectory, markdownParser, Build);

		if (file.Extension != ".toml")
			return null;

		// Initialize version lock on first TOML file (lazy loading)
		if (!_versionLockInitialized)
		{
			DetectionRule.InitializeVersionLock(Build.ReadFileSystem, Build.DocumentationCheckoutDirectory);
			_versionLockInitialized = true;
		}

		return new DetectionRuleFile(file, Build.DocumentationSourceDirectory, markdownParser, Build);
	}

	public MarkdownFile? CreateMarkdownFile(IFileInfo file, IDirectoryInfo sourceDirectory, MarkdownParser markdownParser)
	{
		if (file.Name == "index.md")
			return new DetectionRuleOverviewFile(file, sourceDirectory, markdownParser, Build);
		// Physical deprecated_file on disk takes priority over the synthetic path
		if (DeprecatedOverviewFileNames.Contains(file.Name))
			return new DeprecatedDetectionRuleOverviewFile(file, sourceDirectory, markdownParser, Build);
		return null;
	}

	/// <inheritdoc />
	public void VisitNavigation(INavigationItem navigation, IDocumentationFile model)
	{
		if (navigation is not VirtualFileNavigation<MarkdownFile> node)
			return;

		var ruleNavigations = node.NavigationItems
			.OfType<ILeafNavigationItem<IDocumentationFile>>()
			.Where(n => n.Model is DetectionRuleFile)
			.ToArray();

		switch (model)
		{
			case DetectionRuleOverviewFile overview:
				overview.RuleNavigations = ruleNavigations;
				break;
			case DeprecatedDetectionRuleOverviewFile deprecatedOverview:
				deprecatedOverview.RuleNavigations = ruleNavigations;
				break;
		}
	}

	public bool TryGetDocumentationFileBySlug(DocumentationSet documentationSet, string slug, out DocumentationFile? documentationFile)
	{
		var tomlFile = $"../{slug}.toml";
		var filePath = new FilePath(tomlFile, Build.DocumentationSourceDirectory);
		return documentationSet.Files.TryGetValue(filePath, out documentationFile);
	}

	public IReadOnlyCollection<(IFileInfo, DocumentationFile)> ScanDocumentationFiles(Func<IFileInfo, IDirectoryInfo, DocumentationFile> defaultFileHandling)
	{
		var overviewRefs = GetAllDetectionRuleOverviewRefs(Build.ConfigurationYaml.TableOfContents).ToArray();

		// Pass each overviewRef as a single-element sequence so the switch case that
		// checks DeprecatedSiblingRef is triggered, picking up both active and deprecated rules.
		var rules = overviewRefs
			.SelectMany(r => GetAllDetectionRuleRefs([r]))
			.ToArray();

		var result = rules
			.Select(r => (r.FileInfo, defaultFileHandling(r.FileInfo, r.FileInfo.Directory!)))
			.ToList();

		// Pre-register synthetic deprecated overview files for overviews without a physical file.
		// When the physical file exists it is already picked up by the normal source directory scan.
		foreach (var overviewRef in overviewRefs)
		{
			var deprecatedFileName = overviewRef.DeprecatedFile ?? "deprecated-detection-rules.md";
			var syntheticPath = Build.ReadFileSystem.Path.Join(Build.DocumentationSourceDirectory.FullName, deprecatedFileName);
			var syntheticFileInfo = Build.ReadFileSystem.FileInfo.New(syntheticPath);

			if (syntheticFileInfo.Exists)
				continue; // physical file handled by normal scan

			// Only register if this overview actually has deprecated rule children (now in DeprecatedSiblingRef)
			var hasDeprecatedChildren = overviewRef.DeprecatedSiblingRef?.Children.OfType<DetectionRuleRef>().Any() == true;

			if (!hasDeprecatedChildren)
				continue;

			var deprecatedFile = defaultFileHandling(syntheticFileInfo, Build.DocumentationSourceDirectory);
			if (deprecatedFile is not ExcludedFile)
				result.Add((syntheticFileInfo, deprecatedFile));
		}

		if (result.Count == 0)
			return [];

		return result.ToArray();
	}

	// Finds all DetectionRuleOverviewRef instances at any depth in the TOC tree
	private static IEnumerable<DetectionRuleOverviewRef> GetAllDetectionRuleOverviewRefs(IEnumerable<ITableOfContentsItem> items) =>
		items.SelectMany(item => item switch
		{
			DetectionRuleOverviewRef r => [r],
			FileRef fr => GetAllDetectionRuleOverviewRefs(fr.Children),
			_ => []
		});

	// Finds all DetectionRuleRef instances at any depth within a set of TOC items.
	// Also scans DeprecatedSiblingRef on DetectionRuleOverviewRef since deprecated rules
	// are no longer nested in Children — they live in the sibling's Children instead.
	private static IEnumerable<DetectionRuleRef> GetAllDetectionRuleRefs(IEnumerable<ITableOfContentsItem> items) =>
		items.SelectMany(item => item switch
		{
			DetectionRuleRef dr => [dr],
			DetectionRuleOverviewRef r when r.DeprecatedSiblingRef is { } dep =>
				GetAllDetectionRuleRefs(r.Children).Concat(GetAllDetectionRuleRefs(dep.Children)),
			FileRef fr => GetAllDetectionRuleRefs(fr.Children),
			_ => []
		});
}
