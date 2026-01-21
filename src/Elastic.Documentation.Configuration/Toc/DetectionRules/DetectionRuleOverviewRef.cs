// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

namespace Elastic.Documentation.Configuration.Toc.DetectionRules;

public record DetectionRuleOverviewRef : FileRef
{
	public IReadOnlyCollection<string> DetectionRuleFolders { get; }

	public DetectionRuleOverviewRef(
		string pathRelativeToDocumentationSet,
		string pathRelativeToContainer,
		IReadOnlyCollection<string> detectionRulesFolders,
		IReadOnlyCollection<ITableOfContentsItem> children,
		string context
	) : base(pathRelativeToDocumentationSet, pathRelativeToContainer, false, children, context)
	{
		PathRelativeToDocumentationSet = pathRelativeToDocumentationSet;
		PathRelativeToContainer = pathRelativeToContainer;
		DetectionRuleFolders = detectionRulesFolders;
		Children = children;
		Context = context;
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
}
