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
	private BuildContext Build { get; } = build;

	public IDocumentationFileExporter? FileExporter { get; } = new RuleDocumentationFileExporter(build.ReadFileSystem, build.WriteFileSystem);

	public DocumentationFile? CreateDocumentationFile(IFileInfo file, MarkdownParser markdownParser) =>
		file.Extension != ".toml" ? null : new DetectionRuleFile(file, Build.DocumentationSourceDirectory, markdownParser, Build);

	public MarkdownFile? CreateMarkdownFile(IFileInfo file, IDirectoryInfo sourceDirectory, MarkdownParser markdownParser) =>
		file.Name != "index.md" ? null : new DetectionRuleOverviewFile(file, sourceDirectory, markdownParser, Build);

	/// <inheritdoc />
	public void VisitNavigation(INavigationItem navigation, IDocumentationFile model)
	{
		if (model is not DetectionRuleOverviewFile overview)
			return;
		if (navigation is not VirtualFileNavigation<MarkdownFile> node)
			return;
		var detectionRuleNavigations = node.NavigationItems
			.OfType<ILeafNavigationItem<IDocumentationFile>>()
			.Where(n => n.Model is DetectionRuleFile)
			.ToArray();

		overview.RuleNavigations = detectionRuleNavigations;
	}

	public bool TryGetDocumentationFileBySlug(DocumentationSet documentationSet, string slug, out DocumentationFile? documentationFile)
	{
		var tomlFile = $"../{slug}.toml";
		var filePath = new FilePath(tomlFile, Build.DocumentationSourceDirectory);
		return documentationSet.Files.TryGetValue(filePath, out documentationFile);
	}

	public IReadOnlyCollection<(IFileInfo, DocumentationFile)> ScanDocumentationFiles(Func<IFileInfo, IDirectoryInfo, DocumentationFile> defaultFileHandling)
	{
		var rules = Build.ConfigurationYaml.TableOfContents.OfType<FileRef>().First().Children.OfType<DetectionRuleRef>().ToArray();
		if (rules.Length == 0)
			return [];

		return rules.Select(r => (r.FileInfo, defaultFileHandling(r.FileInfo, r.FileInfo.Directory!))).ToArray();
	}

}
