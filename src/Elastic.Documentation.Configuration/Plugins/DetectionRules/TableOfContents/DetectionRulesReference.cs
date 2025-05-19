// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.TableOfContents;
using Elastic.Documentation.Navigation;

namespace Elastic.Documentation.Configuration.Plugins.DetectionRules.TableOfContents;

public record RuleOverviewReference : FileReference
{

	public IReadOnlyCollection<string> DetectionRuleFolders { get; init; }

	private string ParentPath { get; }

	public RuleOverviewReference(
		ITableOfContentsScope tableOfContentsScope,
		string overviewFilePath,
		string parentPath,
		ConfigurationFile configuration,
		IDocumentationContext context,
		IReadOnlyCollection<string> detectionRuleFolders
	)
		: base(tableOfContentsScope, overviewFilePath, false, [])
	{
		ParentPath = parentPath;
		DetectionRuleFolders = detectionRuleFolders;
		Children = CreateTableOfContentItems(configuration, context);
	}

	private IReadOnlyCollection<ITocItem> CreateTableOfContentItems(ConfigurationFile configuration, IDocumentationContext context)
	{
		var tocItems = new List<ITocItem>();
		foreach (var detectionRuleFolder in DetectionRuleFolders)
		{
			var children = ReadDetectionRuleFolder(configuration, context, detectionRuleFolder);
			tocItems.AddRange(children);
		}

		return tocItems
			.OrderBy(d => d is RuleReference r ? r.Rule.Name : null, StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	private IReadOnlyCollection<ITocItem> ReadDetectionRuleFolder(ConfigurationFile configuration, IDocumentationContext context, string detectionRuleFolder)
	{
		var detectionRulesFolder = Path.Combine(ParentPath, detectionRuleFolder).TrimStart(Path.DirectorySeparatorChar);
		var fs = context.ReadFileSystem;
		var sourceDirectory = context.DocumentationSourceDirectory;
		var path = fs.DirectoryInfo.New(fs.Path.GetFullPath(fs.Path.Combine(sourceDirectory.FullName, detectionRulesFolder)));
		IReadOnlyCollection<ITocItem> children = path
			.EnumerateFiles("*.*", SearchOption.AllDirectories)
			.Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden) && !f.Attributes.HasFlag(FileAttributes.System))
			.Where(f => !f.Directory!.Attributes.HasFlag(FileAttributes.Hidden) && !f.Directory!.Attributes.HasFlag(FileAttributes.System))
			.Where(f => f.Extension is ".md" or ".toml")
			.Where(f => f.Name != "README.md")
			.Where(f => !f.FullName.Contains($"{Path.DirectorySeparatorChar}_deprecated{Path.DirectorySeparatorChar}"))
			.Select(f =>
			{
				var relativePath = Path.GetRelativePath(sourceDirectory.FullName, f.FullName);
				if (f.Extension == ".toml")
				{
					var rule = DetectionRule.From(f);
					return new RuleReference(configuration, relativePath, detectionRuleFolder, true, [], rule);
				}

				return new FileReference(configuration, relativePath, false, []);
			})
			.ToArray();

		return children;
	}
}
