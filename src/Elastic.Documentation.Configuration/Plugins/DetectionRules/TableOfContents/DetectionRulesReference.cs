// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Navigation;

namespace Elastic.Documentation.Configuration.Plugins.DetectionRules.TableOfContents;

public record RuleOverviewReference : FileRef
{

	public IReadOnlyCollection<string> DetectionRuleFolders { get; init; }

	private string ParentPath { get; }
	private string TocContext { get; }

	public RuleOverviewReference(
		string overviewFilePath,
		string parentPath,
		ConfigurationFile configuration,
		IDocumentationSetContext context,
		IReadOnlyCollection<string> detectionRuleFolders,
		string tocContext
	)
		: base(overviewFilePath, false, [], tocContext)
	{
		ParentPath = parentPath;
		TocContext = tocContext;
		DetectionRuleFolders = detectionRuleFolders;
		Children = CreateTableOfContentItems(configuration, context);
	}

	private IReadOnlyCollection<ITableOfContentsItem> CreateTableOfContentItems(ConfigurationFile configuration, IDocumentationSetContext context)
	{
		_ = configuration; // Keep parameter for now for compatibility
		var tocItems = new List<ITableOfContentsItem>();
		foreach (var detectionRuleFolder in DetectionRuleFolders)
		{
			var children = ReadDetectionRuleFolder(context, detectionRuleFolder);
			tocItems.AddRange(children);
		}

		return tocItems
			.OrderBy(d => d is RuleReference r ? r.Rule.Name : null, StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	private IReadOnlyCollection<ITableOfContentsItem> ReadDetectionRuleFolder(IDocumentationSetContext context, string detectionRuleFolder)
	{
		var detectionRulesFolder = System.IO.Path.Combine(ParentPath, detectionRuleFolder).TrimStart(System.IO.Path.DirectorySeparatorChar);
		var fs = context.ReadFileSystem;
		var sourceDirectory = context.DocumentationSourceDirectory;
		var directory = fs.DirectoryInfo.New(fs.Path.GetFullPath(fs.Path.Combine(sourceDirectory.FullName, detectionRulesFolder)));
		IReadOnlyCollection<ITableOfContentsItem> children = directory
			.EnumerateFiles("*.*", SearchOption.AllDirectories)
			.Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden) && !f.Attributes.HasFlag(FileAttributes.System))
			.Where(f => !f.Directory!.Attributes.HasFlag(FileAttributes.Hidden) && !f.Directory!.Attributes.HasFlag(FileAttributes.System))
			.Where(f => f.Extension is ".md" or ".toml")
			.Where(f => f.Name != "README.md")
			.Where(f => !f.FullName.Contains($"{System.IO.Path.DirectorySeparatorChar}_deprecated{System.IO.Path.DirectorySeparatorChar}"))
			.Select(f =>
			{
				var relativePath = System.IO.Path.GetRelativePath(sourceDirectory.FullName, f.FullName);
				if (f.Extension == ".toml")
				{
					var rule = DetectionRule.From(f);
					return new RuleReference(relativePath, detectionRuleFolder, true, [], rule, TocContext);
				}

				return new FileRef(relativePath, false, [], TocContext);
			})
			.ToArray();

		return children;
	}
}
