// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.IO.Navigation;

namespace Elastic.Markdown.Extensions.DetectionRules;

public class DetectionRulesDocsBuilderExtension(BuildContext build) : IDocsBuilderExtension
{
	private BuildContext Build { get; } = build;

	public bool InjectsIntoNavigation(ITocItem tocItem) => false;

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
	}

	public void Visit(DocumentationFile file, ITocItem tocItem)
	{
		// ensure the file has an instance of the rule the reference parsed.
		if (file is DetectionRuleFile df && tocItem is RuleReference r)
			df.Rule = r.Rule;
	}

	public DocumentationFile? CreateDocumentationFile(IFileInfo file, IDirectoryInfo sourceDirectory, DocumentationSet documentationSet)
	{
		if (file.Extension != ".toml")
			return null;

		return new DetectionRuleFile(file, Build.DocumentationSourceDirectory, documentationSet.MarkdownParser, Build, documentationSet);
	}

	public bool TryGetDocumentationFileBySlug(DocumentationSet documentationSet, string slug, out DocumentationFile? documentationFile)
	{
		var tomlFile = $"../{slug}.toml";
		return documentationSet.FlatMappedFiles.TryGetValue(tomlFile, out documentationFile);
	}

	public IReadOnlyCollection<DocumentationFile> ScanDocumentationFiles(
		Func<BuildContext, IDirectoryInfo, DocumentationFile[]> scanDocumentationFiles,
		Func<IFileInfo, IDirectoryInfo, DocumentationFile> defaultFileHandling
	)
	{
		var rules = Build.Configuration.TableOfContents.OfType<FileReference>().First().Children.OfType<RuleReference>().ToArray();
		if (rules.Length == 0)
			return [];

		var sourcePath = Path.GetFullPath(Path.Combine(Build.DocumentationSourceDirectory.FullName, rules[0].SourceDirectory));
		var sourceDirectory = Build.ReadFileSystem.DirectoryInfo.New(sourcePath);
		return rules.Select(r =>
		{
			var file = Build.ReadFileSystem.FileInfo.New(Path.Combine(sourceDirectory.FullName, r.Path));
			return defaultFileHandling(file, sourceDirectory);

		}).ToArray();
	}

	public IReadOnlyCollection<ITocItem> CreateTableOfContentItems(ConfigurationFile configuration, string parentPath,
		string[] detectionRuleFolders,
		HashSet<string> files)
	{
		var tocItems = new List<ITocItem>();
		foreach (var detectionRuleFolder in detectionRuleFolders)
		{
			var children = ReadDetectionRuleFolder(configuration, parentPath, files, detectionRuleFolder);
			tocItems.AddRange(children);
		}
		return tocItems
			.OrderBy(d => d is RuleReference r ? r.Rule.Name : null, StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	private IReadOnlyCollection<ITocItem> ReadDetectionRuleFolder(ConfigurationFile configuration, string parentPath, HashSet<string> files,
		string detectionRuleFolder)
	{
		var detectionRulesFolder = Path.Combine(parentPath, detectionRuleFolder).TrimStart(Path.DirectorySeparatorChar);
		var fs = Build.ReadFileSystem;
		var sourceDirectory = Build.DocumentationSourceDirectory;
		var path = fs.DirectoryInfo.New(fs.Path.GetFullPath(fs.Path.Combine(sourceDirectory.FullName, detectionRulesFolder)));
		IReadOnlyCollection<ITocItem> children = path
			.EnumerateFiles("*.*", SearchOption.AllDirectories)
			.Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden) && !f.Attributes.HasFlag(FileAttributes.System))
			.Where(f => !f.Directory!.Attributes.HasFlag(FileAttributes.Hidden) && !f.Directory!.Attributes.HasFlag(FileAttributes.System))
			.Where(f => f.Extension is ".md" or ".toml")
			.Where(f => f.Name != "README.md")
			.Where(f => !f.FullName.Contains("_deprecated"))
			.Select(f =>
			{
				var relativePath = Path.GetRelativePath(sourceDirectory.FullName, f.FullName);
				if (f.Extension == ".toml")
				{
					var rule = DetectionRule.From(f);
					return new RuleReference(configuration, relativePath, detectionRuleFolder, true, [], rule);
				}

				_ = files.Add(relativePath);
				return new FileReference(configuration, relativePath, true, false, []);
			})
			.ToArray();

		return children;
	}
}
