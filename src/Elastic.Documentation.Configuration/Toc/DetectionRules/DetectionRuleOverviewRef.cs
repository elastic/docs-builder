// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

namespace Elastic.Documentation.Configuration.Toc.DetectionRules;

public record DetectionRuleOverviewRef : FileRef
{
	public IReadOnlyCollection<string> DetectionRuleFolders { get; }

	/// <summary>Optional path to a markdown file whose content prefixes the deprecated rules listing page.</summary>
	public string? DeprecatedFile { get; init; }

	/// <summary>
	/// The resolved deprecated-rules overview FileRef that should appear as a sibling to this ref in the nav.
	/// Set by <c>ResolveRuleOverviewReference</c> when a <c>_deprecated</c> subfolder is detected.
	/// </summary>
	public FileRef? DeprecatedSiblingRef { get; init; }

	public DetectionRuleOverviewRef(
		string pathRelativeToDocumentationSet,
		string pathRelativeToContainer,
		IReadOnlyCollection<string> detectionRulesFolders,
		IReadOnlyCollection<ITableOfContentsItem> children,
		string context,
		string? deprecatedFile = null
	) : base(pathRelativeToDocumentationSet, pathRelativeToContainer, false, children, context)
	{
		PathRelativeToDocumentationSet = pathRelativeToDocumentationSet;
		PathRelativeToContainer = pathRelativeToContainer;
		DetectionRuleFolders = detectionRulesFolders;
		Children = children;
		Context = context;
		DeprecatedFile = deprecatedFile;
	}

	public static IReadOnlyCollection<ITableOfContentsItem> CreateTableOfContentItems(IReadOnlyCollection<IDirectoryInfo> sourceFolders, string context, IDirectoryInfo baseDirectory)
	{
		var tocItems = new List<ITableOfContentsItem>();
		foreach (var detectionRuleFolder in sourceFolders)
		{
			var children = ReadDetectionRuleFolder(detectionRuleFolder, context, baseDirectory);
			tocItems.AddRange(children);
		}

		return tocItems
			.ToArray();
	}

	public static IReadOnlyCollection<ITableOfContentsItem> CreateDeprecatedTableOfContentItems(IReadOnlyCollection<IDirectoryInfo> sourceFolders, string context, IDirectoryInfo baseDirectory)
	{
		var tocItems = new List<ITableOfContentsItem>();
		foreach (var detectionRuleFolder in sourceFolders)
		{
			var children = ReadDeprecatedDetectionRuleFolder(detectionRuleFolder, context, baseDirectory);
			tocItems.AddRange(children);
		}

		return tocItems.ToArray();
	}

	private static IReadOnlyCollection<ITableOfContentsItem> ReadDetectionRuleFolder(IDirectoryInfo directory, string context, IDirectoryInfo baseDirectory)
	{
		IReadOnlyCollection<ITableOfContentsItem> children = directory
			.EnumerateFiles("*.*", SearchOption.AllDirectories)
			.Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden) && !f.Attributes.HasFlag(FileAttributes.System))
			.Where(f => !f.Directory!.Attributes.HasFlag(FileAttributes.Hidden) && !f.Directory!.Attributes.HasFlag(FileAttributes.System))
			// skip symlinks
			.Where(f => f.LinkTarget == null)
			.Where(f => f.Extension is ".md" or ".toml")
			.Where(f => f.Name != "README.md")
			.Where(f => !f.FullName.Contains($"{Path.DirectorySeparatorChar}_deprecated{Path.DirectorySeparatorChar}"))
			.Select(f =>
			{
				// baseDirectory is 'docs' rules live relative to docs parent '/'
				var relativePath = Path.GetRelativePath(baseDirectory.Parent!.FullName, f.FullName);
				if (f.Extension == ".toml")
					return new DetectionRuleRef(f, relativePath, context);

				return new FileRef(relativePath, relativePath, false, [], context);
			})
			.ToArray();

		return children;
	}

	private static IReadOnlyCollection<ITableOfContentsItem> ReadDeprecatedDetectionRuleFolder(IDirectoryInfo directory, string context, IDirectoryInfo baseDirectory)
	{
		IReadOnlyCollection<ITableOfContentsItem> children = directory
			.EnumerateFiles("*.*", SearchOption.AllDirectories)
			.Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden) && !f.Attributes.HasFlag(FileAttributes.System))
			.Where(f => !f.Directory!.Attributes.HasFlag(FileAttributes.Hidden) && !f.Directory!.Attributes.HasFlag(FileAttributes.System))
			// skip symlinks
			.Where(f => f.LinkTarget == null)
			.Where(f => f.Extension == ".toml")
			// only include files inside _deprecated subdirectories
			.Where(f => f.FullName.Contains($"{Path.DirectorySeparatorChar}_deprecated{Path.DirectorySeparatorChar}"))
			.Select(f =>
			{
				var relativePath = Path.GetRelativePath(baseDirectory.Parent!.FullName, f.FullName);
				return (ITableOfContentsItem)new DetectionRuleRef(f, relativePath, context);
			})
			.ToArray();

		return children;
	}
}
