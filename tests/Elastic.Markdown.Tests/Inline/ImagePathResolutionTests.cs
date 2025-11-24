// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Navigation;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Myst.InlineParsers;
using Elastic.Markdown.Tests;
using FluentAssertions;
using Xunit;

namespace Elastic.Markdown.Tests.Inline;

public class ImagePathResolutionTests(ITestOutputHelper output)
{
	[Fact]
	public async Task UpdateRelativeUrlUsesNavigationPathWhenAssemblerBuildEnabled()
	{
		const string relativeAssetPath = "images/pic.png";
		var nonAssemblerResult = await ResolveUrlForBuildMode(relativeAssetPath, assemblerBuild: false, pathPrefix: "this-is-not-relevant");
		var assemblerResult = await ResolveUrlForBuildMode(relativeAssetPath, assemblerBuild: true, pathPrefix: "platform");

		nonAssemblerResult.Should().AllBe("/docs/setup/images/pic.png");
		assemblerResult.Should().AllBe("/docs/platform/setup/images/pic.png");
	}

	[Fact]
	public async Task UpdateRelativeUrlWithoutPathPrefixKeepsGlobalPrefix()
	{
		var relativeAssetPath = "images/funny-image.png";
		var assemblerResult = await ResolveUrlForBuildMode(relativeAssetPath, assemblerBuild: true, pathPrefix: null);

		assemblerResult.Should().AllBe("/docs/setup/images/funny-image.png");
	}

	[Fact]
	public async Task UpdateRelativeUrlAppliesCustomPathPrefix()
	{
		var relativeAssetPath = "images/image.png";
		var assemblerResult = await ResolveUrlForBuildMode(relativeAssetPath, assemblerBuild: true, pathPrefix: "custom");

		assemblerResult.Should().AllBe("/docs/custom/setup/images/image.png");
	}

	/// <summary>
	/// Resolves a relative asset URL the same way the assembler would for a single markdown file, using the provided navigation path prefix.
	/// </summary>
	private async Task<string[]> ResolveUrlForBuildMode(string relativeAssetPath, bool assemblerBuild, string? pathPrefix)
	{
		const string guideRelativePath = "setup/guide.md";
		var files = new Dictionary<string, MockFileData>
		{
			["docs/docset.yml"] = new(
				$"""
				project: test
				toc:
				  - file: index.md
				  - file: {guideRelativePath}
				"""
			),
			["docs/index.md"] = new(
				$"""
				 # Home

				 ![Alt](setup/{relativeAssetPath})
			"""
			),
			["docs/" + guideRelativePath] = new(
				$"""
				 # Guide

				 ![Alt]({relativeAssetPath})
				 """
			),
			["docs/setup/" + relativeAssetPath] = new([])
		};

		var fileSystem = new MockFileSystem(files, new MockFileSystemOptions
		{
			CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
		});

		var collector = new TestDiagnosticsCollector(output);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);

		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var buildContext = new BuildContext(collector, fileSystem, configurationContext)
		{
			UrlPathPrefix = "/docs",
			AssemblerBuild = assemblerBuild
		};

		var documentationSet = new DocumentationSet(buildContext, new TestLoggerFactory(output), new TestCrossLinkResolver());

		await documentationSet.ResolveDirectoryTree(TestContext.Current.CancellationToken);

		// Normalize path for cross-platform compatibility (Windows uses backslashes)
		(string, string)[] pathsToTest = [(guideRelativePath.Replace('/', Path.DirectorySeparatorChar), relativeAssetPath), ("index.md", $"setup{Path.DirectorySeparatorChar}{relativeAssetPath}")];
		List<string> toReturn = [];

		foreach (var normalizedPath in pathsToTest)
		{
			if (documentationSet.TryFindDocumentByRelativePath(normalizedPath.Item1) is not MarkdownFile markdownFile)
				throw new InvalidOperationException($"Failed to resolve markdown file for test. Tried path: {normalizedPath}");

			var navigationUrl = BuildNavigationUrl(pathPrefix, normalizedPath.Item1);
			// For assembler builds DocumentationSetNavigation seeds MarkdownNavigationLookup with navigation items whose Url already
			// includes the computed path_prefix. To exercise the same branch in isolation, inject a stub navigation entry with the
			// expected Url (and minimal metadata for the surrounding API contract).
			_ = documentationSet.NavigationDocumentationFileLookup.Remove(markdownFile);
			documentationSet.NavigationDocumentationFileLookup.Add(markdownFile, new NavigationItemStub(navigationUrl));
			documentationSet.NavigationDocumentationFileLookup.TryGetValue(markdownFile, out var navigation).Should()
				.BeTrue("navigation lookup should contain current page");
			navigation?.Url.Should().Be(navigationUrl);

			var parserState = new ParserState(buildContext)
			{
				MarkdownSourcePath = markdownFile.SourceFile,
				YamlFrontMatter = null,
				CrossLinkResolver = documentationSet.CrossLinkResolver,
				TryFindDocument = file => documentationSet.TryFindDocument(file),
				TryFindDocumentByRelativePath = path => documentationSet.TryFindDocumentByRelativePath(path),
				NavigationTraversable = documentationSet
			};

			var context = new ParserContext(parserState);
			context.TryFindDocument(context.MarkdownSourcePath).Should().BeSameAs(markdownFile);
			context.Build.AssemblerBuild.Should().Be(assemblerBuild);

			toReturn.Add(DiagnosticLinkInlineParser.UpdateRelativeUrl(context, normalizedPath.Item2));

		}

		await collector.StopAsync(TestContext.Current.CancellationToken);

		return toReturn.ToArray();
	}

	/// <summary>
	/// Helper that mirrors the assembler's path-prefix handling in <c>DocumentationSetNavigation</c>:
	/// combines the relative <c>path_prefix</c> from navigation.yml with the markdown path (stripped of ".md") so our stub
	/// navigation item carries the same Url the production code would have provided.
	/// </summary>
	private static string BuildNavigationUrl(string? pathPrefix, string docRelativePath)
	{
		var docPath = docRelativePath.Replace('\\', '/').Trim('/');
		if (docPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
			docPath = docPath[..^3];

		// Handle index.md
		if (docPath.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
			docPath = docPath[..^6];
		else if (docPath.Equals("index", StringComparison.OrdinalIgnoreCase))
			docPath = string.Empty;

		var segments = new List<string>();
		if (!string.IsNullOrWhiteSpace(pathPrefix))
			segments.Add(pathPrefix.Trim('/'));
		if (!string.IsNullOrWhiteSpace(docPath))
			segments.Add(docPath);

		var combined = string.Join('/', segments);
		return "/" + combined.Trim('/');
	}

	/// <summary>
	/// Minimal navigation stub so UpdateRelativeUrl can rely on navigation metadata without constructing the full site navigation tree.
	/// </summary>
	private sealed class NavigationItemStub(string url) : INavigationItem
	{
		private sealed class NavigationModelStub : INavigationModel
		{
		}

		/// <summary>
		/// Simplified root navigation item to satisfy the IRootNavigationItem contract.
		/// </summary>
		private sealed class RootNavigationItemStub : IRootNavigationItem<INavigationModel, INavigationItem>
		{
			/// <summary>
			/// Leaf implementation used by the root stub. Navigation requires both root and leaf nodes present.
			/// </summary>
			private sealed class LeafNavigationItemStub(RootNavigationItemStub root) : ILeafNavigationItem<INavigationModel>
			{
				public string Url => "/";
				public string NavigationTitle => "Root";
				public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = root;
				public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
				public bool Hidden => false;
				public int NavigationIndex { get; set; }
				public INavigationModel Model { get; } = new NavigationModelStub();
			}

			public RootNavigationItemStub() => Index = new LeafNavigationItemStub(this);

			public string Url => "/";
			public string NavigationTitle => "Root";
			public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => this;
			public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
			public bool Hidden => false;
			public int NavigationIndex { get; set; }
			public string Id => "root";
			public ILeafNavigationItem<INavigationModel> Index { get; }
			public IReadOnlyCollection<INavigationItem> NavigationItems { get; private set; } = [];
			public bool IsUsingNavigationDropdown => false;
			public Uri Identifier => new("https://example.test/");
			public void SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) => NavigationItems = navigationItems;
		}

		private static readonly RootNavigationItemStub Root = new();

		public string Url { get; } = url;
		public string NavigationTitle => "Stub";
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => Root;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
		public bool Hidden => false;
		public int NavigationIndex { get; set; }
	}
}
