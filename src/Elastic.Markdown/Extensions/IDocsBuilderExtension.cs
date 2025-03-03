// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Markdown.Extensions.DetectionRules;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.IO.Navigation;

namespace Elastic.Markdown.Extensions;

public interface IDocsBuilderExtension
{
	bool Processes(ITocItem tocItem);

	void CreateNavigationItem(
		DocumentationGroup? parent,
		ITocItem tocItem,
		NavigationLookups lookups,
		List<DocumentationGroup> groups,
		List<INavigationItem> navigationItems,
		int depth,
		ref int fileIndex,
		int index
	);

	DocumentationFile? CreateDocumentationFile(IFileInfo file, IDirectoryInfo sourceDirectory);
}

public class DetectionRulesDocsBuilderExtension(BuildContext build, DocumentationSet set) : IDocsBuilderExtension
{
	public string Name { get; } = "detection-rules";
	public BuildContext Build { get; } = build;
	public bool Processes(ITocItem tocItem) => tocItem is RulesFolderReference;

	public void CreateNavigationItem(
		DocumentationGroup? parent,
		ITocItem tocItem,
		NavigationLookups lookups,
		List<DocumentationGroup> groups,
		List<INavigationItem> navigationItems,
		int depth,
		ref int fileIndex,
		int index)
	{
		var detectionRulesFolder = (RulesFolderReference)tocItem;
		var children = detectionRulesFolder.Children;
		var group = new DocumentationGroup(Build, lookups with { TableOfContents = children }, ref fileIndex, depth + 1)
		{
			Parent = parent
		};
		groups.Add(group);
		navigationItems.Add(new GroupNavigation(index, depth, group));
	}

	public DocumentationFile? CreateDocumentationFile(IFileInfo file, IDirectoryInfo sourceDirectory)
	{
		if (file.Extension != ".toml")
			return null;

		return new DetectionRuleFile(file, Build.DocumentationSourceDirectory, set.MarkdownParser, Build, set);
	}
}
