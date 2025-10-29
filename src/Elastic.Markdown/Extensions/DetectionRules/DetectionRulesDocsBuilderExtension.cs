// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Configuration.Plugins.DetectionRules.TableOfContents;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.NewNavigation;
using Elastic.Markdown.Myst;

namespace Elastic.Markdown.Extensions.DetectionRules;

public class DetectionRulesDocsBuilderExtension(BuildContext build) : IDocsBuilderExtension
{
	private BuildContext Build { get; } = build;

	public IDocumentationFileExporter? FileExporter { get; } = new RuleDocumentationFileExporter(build.ReadFileSystem, build.WriteFileSystem);

	private DetectionRuleOverviewFile? _overviewFile;
	public void Visit(DocumentationFile file, ITableOfContentsItem tocItem)
	{
		// TODO the parsing of rules should not happen at ITocItem reading time.
		// ensure the file has an instance of the rule the reference parsed.
		if (file is DetectionRuleFile df && tocItem is RuleReference r)
		{
			df.Rule = r.Rule;
			_overviewFile?.AddDetectionRuleFile(df, r);

		}

		if (file is DetectionRuleOverviewFile of && tocItem is RuleOverviewReference or)
		{
			var rules = or.Children.OfType<RuleReference>().ToArray();
			of.Rules = rules;
			_overviewFile = of;
		}
	}

	public DocumentationFile? CreateDocumentationFile(IFileInfo file, MarkdownParser markdownParser)
	{
		if (file.Extension != ".toml")
			return null;

		return new DetectionRuleFile(file, Build.DocumentationSourceDirectory, markdownParser, Build);
	}

	public MarkdownFile? CreateMarkdownFile(IFileInfo file, IDirectoryInfo sourceDirectory, MarkdownParser markdownParser) =>
		file.Name == "index.md"
			? new DetectionRuleOverviewFile(file, sourceDirectory, markdownParser, Build)
			: null;

	public bool TryGetDocumentationFileBySlug(DocumentationSet documentationSet, string slug, out DocumentationFile? documentationFile)
	{
		var tomlFile = $"../{slug}.toml";
		var filePath = new FilePath(tomlFile, Build.DocumentationSourceDirectory);
		return documentationSet.Files.TryGetValue(filePath, out documentationFile);
	}

	public IReadOnlyCollection<(IFileInfo, DocumentationFile)> ScanDocumentationFiles(
		Func<IFileInfo, IDirectoryInfo, DocumentationFile> defaultFileHandling
	)
	{
		var rules = Build.ConfigurationYaml.TableOfContents.OfType<FileRef>().First().Children.OfType<RuleReference>().ToArray();
		if (rules.Length == 0)
			return [];

		var sourcePath = Path.GetFullPath(Path.Combine(Build.DocumentationSourceDirectory.FullName, rules[0].SourceDirectory));
		var sourceDirectory = Build.ReadFileSystem.DirectoryInfo.New(sourcePath);
		return rules.Select(r =>
		{
			var file = Build.ReadFileSystem.FileInfo.New(Path.Combine(sourceDirectory.FullName, r.RelativePathRelativeToDocumentationSet));
			return (file, defaultFileHandling(file, sourceDirectory));

		}).ToArray();
	}

}
