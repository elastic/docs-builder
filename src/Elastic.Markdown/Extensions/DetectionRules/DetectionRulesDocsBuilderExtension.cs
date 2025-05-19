// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Plugins.DetectionRules.TableOfContents;
using Elastic.Documentation.Configuration.TableOfContents;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Navigation;

namespace Elastic.Markdown.Extensions.DetectionRules;

public class DetectionRulesDocsBuilderExtension(BuildContext build) : IDocsBuilderExtension
{
	private BuildContext Build { get; } = build;

	public IDocumentationFileExporter? FileExporter { get; } = new RuleDocumentationFileExporter(build.ReadFileSystem, build.WriteFileSystem);

	private DetectionRuleOverviewFile? _overviewFile;
	public void Visit(DocumentationFile file, ITocItem tocItem)
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

	public DocumentationFile? CreateDocumentationFile(IFileInfo file, DocumentationSet documentationSet)
	{
		if (file.Extension != ".toml")
			return null;

		return new DetectionRuleFile(file, Build.DocumentationSourceDirectory, documentationSet.MarkdownParser, Build, documentationSet);
	}

	public MarkdownFile? CreateMarkdownFile(IFileInfo file, IDirectoryInfo sourceDirectory, DocumentationSet documentationSet) =>
		file.Name == "index.md"
			? new DetectionRuleOverviewFile(file, sourceDirectory, documentationSet.MarkdownParser, Build, documentationSet)
			: null;

	public bool TryGetDocumentationFileBySlug(DocumentationSet documentationSet, string slug, out DocumentationFile? documentationFile)
	{
		var tomlFile = $"../{slug}.toml";
		return documentationSet.FlatMappedFiles.TryGetValue(tomlFile, out documentationFile);
	}

	public IReadOnlyCollection<DocumentationFile> ScanDocumentationFiles(
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
			var file = Build.ReadFileSystem.FileInfo.New(Path.Combine(sourceDirectory.FullName, r.RelativePath));
			return defaultFileHandling(file, sourceDirectory);

		}).ToArray();
	}

}
