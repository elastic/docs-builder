// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;
using Xunit.Sdk;

namespace Elastic.Authoring.Tests.Framework;

/// <summary>
/// Named class replacing the F# anonymous object expression
/// <c>{ new IDocumentationSetContext with … }</c> used inside
/// <c>CrossLinkResolverAssertions.parseRedirectsYaml</c>.
/// Implements the minimum surface required by <see cref="RedirectFile"/>.
/// </summary>
internal sealed class TestDocumentationSetContext : IDocumentationSetContext
{
	public required IDiagnosticsCollector Collector { get; init; }
	public required IDirectoryInfo DocumentationSourceDirectory { get; init; }
	public required GitCheckoutInformation Git { get; init; }
	public required IDocumentationFileSystem ReadFileSystem { get; init; }
	public required DocumentationWriteFileSystem WriteFileSystem { get; init; }
	public required IFileInfo ConfigurationPath { get; init; }
	public required IDirectoryInfo OutputDirectory { get; init; }
	public required BuildType BuildType { get; init; }
	public required IEnvironmentVariables Environment { get; init; }
}

/// <summary>
/// Port of <c>CrossLinkResolverAssertions</c> from <c>CrossLinkResolverAssertions.fs</c>.
///
/// Deliberately NOT <c>AutoOpen</c>. Callers must qualify as
/// <c>CrossLinkResolverAssertions.ResolvesTo(...)</c> — the F# module was also non-AutoOpen,
/// so this preserves the original scoping intent.
/// </summary>
public static class CrossLinkResolverAssertions
{
	// ────────────────────────────────────────────────────────────────────────────────
	// Private infrastructure — parallels the F# module-level lets
	// ────────────────────────────────────────────────────────────────────────────────

	private static Dictionary<string, LinkRedirect>? ParseRedirectsYaml(string redirectsYamlContent, IDiagnosticsCollector collector)
	{
		var mockFileSystem = new MockFileSystem();
		var indented = redirectsYamlContent.Replace("\r\n", "\n").Replace("\n", "\n  ");
		var fullYaml = $"redirects:\n{indented}";

		const string mockPath = "mock_redirects.yml";
		mockFileSystem.AddFile(mockPath, new MockFileData(fullYaml));
		var mockRedirectsFile = mockFileSystem.FileInfo.New(mockPath);

		var docContext = new TestDocumentationSetContext
		{
			Collector = collector,
			DocumentationSourceDirectory = mockFileSystem.DirectoryInfo.New("/docs"),
			Git = GitCheckoutInformation.Unavailable,
			ReadFileSystem = DocumentationFileSystem.Resolve(
				mockFileSystem.DirectoryInfo.New("/docs"),
				new DocumentationScopeOptions { Inner = mockFileSystem, ConfigurationFile = "/docs/docset.yml" }
			),
			WriteFileSystem = new DocumentationWriteFileSystem(
				mockFileSystem.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName),
				null,
				mockFileSystem
			),
			ConfigurationPath = mockFileSystem.FileInfo.New("mock_docset.yml"),
			OutputDirectory = mockFileSystem.DirectoryInfo.New(".artifacts"),
			BuildType = BuildType.Isolated,
			Environment = SystemEnvironmentVariables.Instance
		};

		var redirectFileParser = new RedirectFile(docContext, mockRedirectsFile);
		return redirectFileParser.Redirects;
	}

	private static FetchedCrossLinks CreateFetchedCrossLinks(
		string redirectsYamlSnippet,
		IReadOnlyDictionary<string, LinkMetadata> linksData,
		string repoName
	)
	{
		var collector = new TestDiagnosticsCollector();
		var redirectRules = ParseRedirectsYaml(redirectsYamlSnippet, collector);

		if (collector.Errors > 0)
			throw new XunitException($"Failed to parse redirects YAML: {collector.Errors} errors");

		var repositoryLinks = new RepositoryLinks
		{
			Origin = GitCheckoutInformation.Unavailable,
			UrlPathPrefix = null,
			Links = new Dictionary<string, LinkMetadata>(linksData),
			CrossLinks = [],
			Redirects = redirectRules
		};

		var declaredRepos = new HashSet<string> { repoName };

		return new FetchedCrossLinks
		{
			DeclaredRepositories = declaredRepos,
			LinkReferences = new Dictionary<string, RepositoryLinks> { [repoName] = repositoryLinks }.ToFrozenDictionary(),
			LinkIndexEntries = FrozenDictionary<string, LinkRegistryEntry>.Empty
		};
	}

	// language=yaml
	private const string RedirectsYaml =
		"""

	  # test scenario 1
	  'testing/redirects/multi-topic-page-1-old.md':
	    to: 'testing/redirects/multi-topic-page-1-new-anchorless.md'
	    anchors: { "!": null }
	    many:
	      - to: 'testing/redirects/multi-topic-page-1-new-topic-a-subpage.md'
	        anchors: {'topic-a-intro': null, 'topic-a-details': 'details-anchor'}
	      - to: 'testing/redirects/multi-topic-page-1-new-topic-b-subpage.md'
	        anchors: {'topic-b-main': 'main-anchor'}
	      - to: 'testing/redirects/multi-topic-page-1-old.md'
	        anchors: {'topic-c-main': 'topic-c-main'}
	  # test scenario 2
	  'testing/redirects/multi-topic-page-2-old.md':
	    to: 'testing/redirects/multi-topic-page-2-old.md'
	    anchors: {} # This means pass through any anchor for the default 'to'
	    many:
	      - to: 'testing/redirects/multi-topic-page-2-new-topic-a-subpage.md'
	        anchors: {'topic-a-intro': 'introduction', 'topic-a-details': null}
	      - to: 'testing/redirects/multi-topic-page-2-new-topic-b-subpage.md'
	        anchors: {'topic-b-main': 'summary', 'topic-b-config': null}
	""";

	private static readonly IReadOnlyDictionary<string, LinkMetadata> MockLinksData = new Dictionary<string, LinkMetadata>
	{
		["testing/redirects/multi-topic-page-1-new-anchorless.md"] = new() { Anchors = null, Hidden = false },
		["testing/redirects/multi-topic-page-1-new-topic-a-subpage.md"] = new()
		{
			Anchors = ["details-anchor", "introduction"],
			Hidden = false
		},
		["testing/redirects/multi-topic-page-1-new-topic-b-subpage.md"] = new() { Anchors = ["main-anchor"], Hidden = false },
		["testing/redirects/multi-topic-page-1-old.md"] = new() { Anchors = ["topic-c-main", "unmatched-anchor"], Hidden = false },
		["testing/redirects/multi-topic-page-2-old.md"] = new()
		{
			Anchors = ["unmatched-anchor", "topic-c-main", "topic-a-intro", "topic-a-details", "topic-b-main", "topic-b-config"],
			Hidden = false
		},
		["testing/redirects/multi-topic-page-2-new-topic-a-subpage.md"] = new() { Anchors = ["introduction", "summary"], Hidden = false },
		["testing/redirects/multi-topic-page-2-new-topic-b-subpage.md"] = new() { Anchors = ["summary"], Hidden = false }
	};

	private const string RepoName = "docs-content";

	private static readonly FetchedCrossLinks FetchedLinks = CreateFetchedCrossLinks(RedirectsYaml, MockLinksData, RepoName);

	private static readonly IUriEnvironmentResolver UriResolver = new IsolatedBuildEnvironmentUriResolver();

	private static readonly string BaseExpectedUrl = $"https://docs-v3-preview.elastic.dev/elastic/{RepoName}/tree/main";

	// ────────────────────────────────────────────────────────────────────────────────
	// Public assertion
	// ────────────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Asserts that <paramref name="inputUrl"/> resolves (via the standard
	/// <see cref="CrossLinkResolver.TryResolve"/> path) to
	/// <c>baseExpectedUrl + expectedPathWithOptionalAnchor</c>.
	/// Port of F# <c>resolvesTo</c>.
	/// </summary>
	public static void ResolvesTo(string inputUrl, string expectedPathWithOptionalAnchor)
	{
		var errors = new List<string>();

		void ErrorEmitter(string msg) => errors.Add(msg);

		var inputUri = new Uri(inputUrl);
		var success = CrossLinkResolver.TryResolve(ErrorEmitter, FetchedLinks, UriResolver, inputUri, out var resolvedUri);

		if (errors.Count > 0)
			throw new XunitException($"Resolution for '{inputUrl}' failed with errors: {string.Join(", ", errors)}");

		success.Should().BeTrue(because: $"TryResolve should succeed for '{inputUrl}'");

		if (resolvedUri is null)
			throw new XunitException($"Resolved URI was null for input '{inputUrl}' even though TryResolve returned true.");

		var expectedFullUrl = BaseExpectedUrl + expectedPathWithOptionalAnchor;
		resolvedUri.ToString().Should().Be(expectedFullUrl);
	}
}
