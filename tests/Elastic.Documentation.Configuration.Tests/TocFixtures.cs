// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.DocumentationSets;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// Fixtures that exercise one concern with a minimal documentation set still have to satisfy the documentation set
/// constraints that every set must: a table of contents with at least one entry, and a page behind every file in it.
/// </summary>
internal static class TocFixtures
{
	/// <summary>Gives <paramref name="docSet"/> a one-page table of contents when it has none, and creates every page it lists.</summary>
	public static void EnsureTableOfContents(DocumentationSetDto docSet, MockFileSystem fileSystem, string docsDirectory)
	{
		if (docSet.TableOfContents.Count == 0)
		{
			docSet.TableOfContents =
			[
				new FileRef(
					DocsetPath.From("index.md"),
					ContainerPath.From("index.md"),
					false,
					[],
					DeclaringFilePath.From(Path.Join(docsDirectory, "_docset.yml"))
				)
			];
		}

		foreach (var file in docSet.TableOfContents.Descendants().OfCase<FileRef>().Where(f => f is not DetectionRuleRef))
		{
			var path = fileSystem.Path.Join(docsDirectory, file.PathRelativeToDocumentationSet.Value);
			if (!fileSystem.File.Exists(path))
				fileSystem.AddFile(path, new MockFileData("# Page"));
		}
	}
}
