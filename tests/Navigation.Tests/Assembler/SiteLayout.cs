// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Floating;
using Elastic.Documentation.Navigation.Isolated;
using Elastic.Documentation.Navigation.Isolated.Node;
using Elastic.Documentation.Testing;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

internal sealed record SiteLayoutResult(SiteNavigation Navigation, SiteNavigationFile File, TestDocumentationSetContext SiteContext)
{
	/// <summary>The tree, then the diagnostics of the site (and of the docsets it loaded), one per line, when there are any.</summary>
	public string ToText()
	{
		var sb = new StringBuilder(NavigationDump.ToText(Navigation));
		var diagnostics = SiteContext.Diagnostics.ToSnapshotText();
		if (diagnostics.Length > 0)
			_ = sb.Append("--- diagnostics\n").Append(diagnostics);
		return sb.ToString();
	}
}

/// <summary>Builds a <see cref="SiteNavigation"/> from the checkouts in a mock file system and a navigation yaml, the way the assembler does.</summary>
internal static class SiteLayout
{
	public static SiteLayoutResult Build(
		MockFileSystem fileSystem,
		string navigationYaml,
		string? sitePrefix = "docs",
		bool navigationPreview = false,
		FloatingSectionRegistry? floatingSections = null,
		bool declareUnmountedAsPhantoms = false,
		params string[] repositories
	)
	{
		var result = BuildCore(fileSystem, navigationYaml, sitePrefix, navigationPreview, floatingSections, repositories);
		if (!declareUnmountedAsPhantoms)
			return result;

		// the hint for a toc nobody mounts is noise in a layout that is about something else
		var unmounted = result
			.Navigation
			.Nodes
			.Keys
			.Where(k => !result.Navigation.DeclaredTableOfContents.Contains(k))
			.Select(k => k.ToString().TrimEnd('/') + (k.AbsolutePath.Trim('/').Length == 0 && k.Host.Length == 0 ? "//" : string.Empty));
		var phantoms = string.Concat(unmounted.Order(StringComparer.Ordinal).Select(u => $"  - toc: {u}\n"));
		if (phantoms.Length == 0)
			return result;
		var yaml = navigationYaml.Contains("phantoms:", StringComparison.Ordinal)
			? navigationYaml.Replace("phantoms:\n", $"phantoms:\n{phantoms}", StringComparison.Ordinal)
			: $"{navigationYaml.TrimEnd()}\nphantoms:\n{phantoms}";
		return BuildCore(fileSystem, yaml, sitePrefix, navigationPreview, floatingSections, repositories);
	}

	private static SiteLayoutResult BuildCore(
		MockFileSystem fileSystem,
		string navigationYaml,
		string? sitePrefix,
		bool navigationPreview,
		FloatingSectionRegistry? floatingSections,
		string[] repositories
	)
	{
		var siteContext = new TestDocumentationSetContext(
			fileSystem,
			fileSystem.DirectoryInfo.New("/docs"),
			fileSystem.DirectoryInfo.New("/output"),
			fileSystem.FileInfo.New("/docs/navigation.yml")
		)
		{ BuildType = BuildType.Assembler };
		var file = SiteNavigationFile.Deserialize(navigationYaml, siteContext.Collector, "/docs/navigation.yml");

		var names = repositories.Length > 0
			? repositories
			: [
				.. fileSystem
					.Directory
					.GetDirectories("/checkouts/current")
					.Select(d => fileSystem.Path.GetFileName(d))
					.Order(StringComparer.Ordinal)
			];
		var sets = new List<IDocumentationSetNavigation>();
		foreach (var name in names)
		{
			var context = SiteNavigationTestFixture.CreateAssemblerContext(
				fileSystem,
				$"/checkouts/current/{name}",
				siteContext.Collector as TestDiagnosticsCollector
			);
			var docsetPath = fileSystem.File.Exists($"/checkouts/current/{name}/docs/docset.yml")
				? $"/checkouts/current/{name}/docs/docset.yml"
				: $"/checkouts/current/{name}/docs/_docset.yml";
			var docset = DocumentationSetLoader.LoadAndResolve(
				context.Collector,
				fileSystem.FileInfo.New(docsetPath),
				new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
			);
			sets.Add(new DocumentationSetNavigation<IDocumentationFile>(docset, context, GenericDocumentationFileFactory.Instance));
		}

		var navigation = new SiteNavigation(file, siteContext, sets, sitePrefix, navigationPreview, floatingSections);
		return new SiteLayoutResult(navigation, file, siteContext);
	}
}
