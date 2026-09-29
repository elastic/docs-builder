// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.DocumentationSets;

namespace Elastic.Documentation.Configuration.Toc.DetectionRules;

/// <summary>Discovers the rule pages beneath a <c>detection_rules</c> entry's folders.</summary>
internal static class DetectionRuleDiscovery
{
	public static IReadOnlyList<TocItem> CreateTableOfContentItems(
		IReadOnlyCollection<IDirectoryInfo> sourceFolders,
		string context,
		IDirectoryInfo baseDirectory
	)
	{
		var tocItems = new List<TocItem>();
		foreach (var detectionRuleFolder in sourceFolders)
		{
			var children = ReadDetectionRuleFolder(detectionRuleFolder, context, baseDirectory);
			tocItems.AddRange(children);
		}

		return tocItems.ToArray();
	}

	public static IReadOnlyList<TocItem> CreateDeprecatedTableOfContentItems(
		IReadOnlyCollection<IDirectoryInfo> sourceFolders,
		string context,
		IDirectoryInfo baseDirectory
	)
	{
		var tocItems = new List<TocItem>();
		foreach (var detectionRuleFolder in sourceFolders)
		{
			var children = ReadDeprecatedDetectionRuleFolder(detectionRuleFolder, context, baseDirectory);
			tocItems.AddRange(children);
		}

		return tocItems.ToArray();
	}

	private static IReadOnlyList<TocItem> ReadDetectionRuleFolder(IDirectoryInfo directory, string context, IDirectoryInfo baseDirectory)
	{
		IReadOnlyList<TocItem> children = directory
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
					return (TocItem)new DetectionRuleRef(f, relativePath, context);

				return new FileRef(relativePath, relativePath, false, [], context);
			})
			.ToArray();

		return children;
	}

	private static IReadOnlyList<TocItem> ReadDeprecatedDetectionRuleFolder(
		IDirectoryInfo directory,
		string context,
		IDirectoryInfo baseDirectory
	)
	{
		IReadOnlyList<TocItem> children = directory
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
				return (TocItem)new DetectionRuleRef(f, relativePath, context);
			})
			.ToArray();

		return children;
	}
}
