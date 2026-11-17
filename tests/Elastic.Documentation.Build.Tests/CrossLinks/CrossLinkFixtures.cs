// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Text;
using Elastic.Documentation.Assembler.Links;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;

namespace Elastic.Documentation.Build.Tests.CrossLinks;

/// <summary>A resolver for one environment shape, named as it reads in the snapshots.</summary>
internal sealed record ResolverEnvironment(string Name, Func<IUriEnvironmentResolver> Create);

/// <summary>
/// Synthetic link indexes and environments for the cross-link snapshot tests. Every repository, page and host is made up.
/// </summary>
internal static class CrossLinkFixtures
{
	private const string ExampleRepoLinks =
		// language=json
		"""
		{
		  "origin": { "branch": "main", "remote": "https://github.com/example-org/example-repo", "ref": "0123456789abcdef0123456789abcdef01234567" },
		  "url_path_prefix": "/elastic/example-repo/tree/main",
		  "cross_links": [],
		  "links": {
		    "index.md": {},
		    "get-started/index.md": { "anchors": [ "intro", "Use-Cases" ] },
		    "guide/page.md": { "anchors": [ "section-one", "section-two" ] },
		    "guide/no-anchors.md": { "anchors": null },
		    "guide/nested/deep.md": { "anchors": [ "deep" ] },
		    "guide/Mixed-Case.md": {},
		    "hidden/secret.md": { "hidden": true, "anchors": [ "inside" ] },
		    "redirect-and-link.md": {}
		  },
		  "redirects": {
		    "old/simple.md": { "to": "guide/page.md" },
		    "old/anchors.md": { "to": "guide/page.md", "anchors": { "a": "section-one", "c": null } },
		    "old/catch-all.md": { "to": "guide/page.md", "anchors": { "!": "!" } },
		    "old/empty-anchors.md": { "to": "guide/page.md", "anchors": {} },
		    "old/no-to.md": { "anchors": { "a": "b" } },
		    "old/many.md": {
		      "to": "get-started/index.md",
		      "many": [
		        { "to": "get-started/index.md", "anchors": { "intro": "intro", "dropped": null } },
		        { "to": "guide/page.md", "anchors": { "section-one": "section-two" } }
		      ]
		    },
		    "old/many-catch-all.md": {
		      "to": "get-started/index.md",
		      "many": [ { "to": "guide/page.md", "anchors": { "!": "!" } } ]
		    },
		    "old/many-only.md": { "many": [ { "to": "guide/page.md", "anchors": { "x": "y" } } ] },
		    "old/many-without-to.md": { "to": "get-started/index.md", "many": [ { "anchors": { "x": "y" } } ] },
		    "old/to-index.md": { "to": "index.md" },
		    "old/to-nested-index.md": { "to": "get-started/index.md" },
		    "old/to-missing-page.md": { "to": "guide/not-a-page.md" },
		    "old/cross.md": { "to": "other-repo://target/page.md" },
		    "old/cross-anchors.md": { "to": "other-repo://target/page.md", "anchors": { "a": "target-anchor" } },
		    "old/cross-missing-target.md": { "to": "other-repo://nope.md" },
		    "old/cross-missing-repo.md": { "to": "unknown-repo://x.md" },
		    "old/cross-declared-only.md": { "to": "declared-only://x.md" },
		    "old/external.md": { "to": "https://example.test/elsewhere" },
		    "redirect-and-link.md": { "to": "guide/page.md" }
		  },
		  "floating": {
		    "api/example-api": {
		      "versions": [
		        { "name": "8", "root": "api/doc/example-api/v8", "hidden": true },
		        { "name": "main", "root": "api/doc/example-api", "current": true }
		      ],
		      "pages": { "": 3, "operation/operation-search": 3, "operation/operation-new": 2, "operation/operation-old": 1, "8": 2 },
		      "aliases": { "8": { "operation/operation-search": [ "operation/operation-search-legacy" ] } }
		    }
		  }
		}
		""";

	private const string OtherRepoLinks =
		// language=json
		"""
		{
		  "origin": { "branch": "main", "remote": "https://github.com/example-org/other-repo", "ref": "fedcba9876543210fedcba9876543210fedcba98" },
		  "url_path_prefix": "/elastic/other-repo/tree/main",
		  "cross_links": [],
		  "links": {
		    "index.md": {},
		    "target/page.md": { "anchors": [ "target-anchor" ] },
		    "target/index.md": {}
		  }
		}
		""";

	private static RepositoryLinks Parse(string json) => RepositoryLinks.Deserialize(json);

	private static LinkRegistryEntry Entry(string repository) =>
		new()
		{
			Repository = repository,
			Path = $"elastic/{repository}/main/links.json",
			Branch = "main",
			ETag = "etag",
			GitReference = "ref"
		};

	/// <summary>
	/// <c>example-repo</c> and <c>other-repo</c> have a <c>links.json</c>, <c>declared-only</c> is declared but has none,
	/// and <c>codex-repo</c> belongs to the codex registry.
	/// </summary>
	public static FetchedCrossLinks CreateIndex(
		FrozenDictionary<string, string>? registryUrls = null,
		FrozenDictionary<string, DocSetRegistry>? registries = null
	) =>
		new()
		{
			DeclaredRepositories = ["example-repo", "other-repo", "declared-only", "codex-repo"],
			LinkReferences = new Dictionary<string, RepositoryLinks>
			{
				["example-repo"] = Parse(ExampleRepoLinks),
				["other-repo"] = Parse(OtherRepoLinks),
				["codex-repo"] = Parse(OtherRepoLinks)
			}.ToFrozenDictionary(),
			LinkIndexEntries = new Dictionary<string, LinkRegistryEntry>
			{
				["example-repo"] = Entry("example-repo"),
				["other-repo"] = Entry("other-repo")
			}.ToFrozenDictionary(),
			RegistryUrlsByRepository = registryUrls,
			RegistryByRepository = registries
		};

	public static IUriEnvironmentResolver Publish(string uri, string? pathPrefix) =>
		new PublishEnvironmentUriResolver(
			new Dictionary<Uri, NavigationTocMapping>
			{
				[new Uri("example-repo://get-started")] = new()
				{
					Source = new Uri("example-repo://get-started"),
					SourcePathPrefix = "docs/start-here"
				},
				[new Uri("example-repo://guide")] = new() { Source = new Uri("example-repo://guide"), SourcePathPrefix = "docs/guide" },
				[new Uri("example-repo://guide/nested")] = new()
				{
					Source = new Uri("example-repo://guide/nested"),
					SourcePathPrefix = "docs/nested-section"
				},
				[new Uri("other-repo://target")] = new() { Source = new Uri("other-repo://target"), SourcePathPrefix = "docs/other/target" }
			}.ToFrozenDictionary(),
			new PublishEnvironment { Uri = uri, PathPrefix = pathPrefix }
		);

	/// <summary>One of each environment shape a <see cref="IUriEnvironmentResolver"/> is created for.</summary>
	public static ResolverEnvironment[] Environments { get; } =
	[
		new("isolated", () => new IsolatedBuildEnvironmentUriResolver()),
		new("codex-absolute", () => new CodexAwareUriResolver(["codex-repo"], useRelativePaths: false)),
		new("codex-relative", () => new CodexAwareUriResolver(["codex-repo"], useRelativePaths: true)),
		new("publish", () => Publish("https://docs.example.test", null)),
		new("publish-prefixed", () => Publish("https://docs.example.test", "docs/9.0")),
		new("publish-prefixed-slash", () => Publish("https://docs.example.test/base/", "/versioned"))
	];

	/// <summary>Appends a line of text. The text is built from strings only, so the culture has no say in it.</summary>
	public static StringBuilder Line(this StringBuilder sb, string text) => sb.AppendLine(text);

	public static string Describe(LinkResolution resolution, Uri input) => resolution switch
	{
		LinkResolved(var uri) => $"resolved {Show(uri)}",
		LinkSynthesized(var uri, var repository) => $"synthesized {Show(uri)} ({repository})",
		LinkSchemeNotDeclared => $"LinkSchemeNotDeclared: {resolution.ToDiagnosticMessage(input)}",
		LinkNotInIndex => $"LinkNotInIndex: {resolution.ToDiagnosticMessage(input)}",
		LinkAnchorNotFound => $"LinkAnchorNotFound: {resolution.ToDiagnosticMessage(input)}",
		LinkRedirectAnchorUnhandled => $"LinkRedirectAnchorUnhandled: {resolution.ToDiagnosticMessage(input)}",
		LinkRedirectRepositoryMissing => $"LinkRedirectRepositoryMissing: {resolution.ToDiagnosticMessage(input)}",
		LinkRedirectTargetMissing => $"LinkRedirectTargetMissing: {resolution.ToDiagnosticMessage(input)}",
		LinkFloatingPageNotFound => $"LinkFloatingPageNotFound: {resolution.ToDiagnosticMessage(input)}",
		LinkResolutionUnavailable => $"LinkResolutionUnavailable: {resolution.ToDiagnosticMessage(input)}"
	};

	private static string Show(Uri uri) => uri.IsAbsoluteUri ? uri.AbsoluteUri : $"(relative) {uri.OriginalString}";

	/// <summary>
	/// One block per input: the input, then what it resolves to in each environment. An input the <see cref="Uri"/> type
	/// rejects is written as such.
	/// </summary>
	public static string Table(FetchedCrossLinks index, IEnumerable<string> inputs, ResolverEnvironment[]? environments = null)
	{
		var sb = new StringBuilder();
		foreach (var input in inputs)
		{
			_ = sb.AppendLine(input);
			if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
			{
				_ = sb.AppendLine("  not an absolute Uri");
				continue;
			}

			foreach (var environment in environments ?? Environments)
			{
				var description = Describe(CrossLinkResolver.Resolve(index, environment.Create(), uri), uri);
				_ = sb.Append("  ").Append(environment.Name.PadRight(24)).AppendLine(description);
			}
		}

		return sb.ToString();
	}
}
