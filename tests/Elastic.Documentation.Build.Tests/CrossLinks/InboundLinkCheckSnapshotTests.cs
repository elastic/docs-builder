// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Assembler.Deploying.Redirects;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Links.InboundLinks;
using Elastic.Documentation.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.Snapshots;

namespace Elastic.Documentation.Build.Tests.CrossLinks;

/// <summary>
/// The inbound link check: every repository's <c>cross_links</c> are resolved against the link index, and what does not
/// resolve is an error. The check is private to <see cref="LinkIndexService"/>, so these tests call it by reflection
/// with a fake link index; the network is never touched.
/// </summary>
public class InboundLinkCheckSnapshotTests
{
	private sealed class CapturingCollector() : DiagnosticsCollector([])
	{
		private readonly List<Diagnostic> _diagnostics = [];

		public IReadOnlyCollection<Diagnostic> Diagnostics => _diagnostics;

		public override void Write(Diagnostic diagnostic)
		{
			IncrementSeverityCount(diagnostic);
			_diagnostics.Add(diagnostic);
		}
	}

	private static RepositoryLinks Repository(string name, string[] crossLinks, string pages) =>
		RepositoryLinks.Deserialize(
			$$"""
			{
			  "origin": { "branch": "main", "remote": "https://github.com/example-org/{{name}}", "ref": "abc" },
			  "url_path_prefix": "/elastic/{{name}}/tree/main",
			  "cross_links": [{{string.Join(", ", crossLinks.Select(l => $"\"{l}\""))}}],
			  "links": {{pages}},
			  "redirects": { "moved.md": { "to": "page.md" } }
			}
			"""
		);

	private static FetchedCrossLinks Index() =>
		new()
		{
			DeclaredRepositories = ["alpha", "beta", "gamma"],
			LinkReferences = new Dictionary<string, RepositoryLinks>
			{
				["alpha"] = Repository(
					"alpha",
					[
						"beta://page.md",
						"beta://page.md#known",
						"beta://page.md#unknown-anchor",
						"beta://missing.md",
						"beta://moved.md",
						"gamma://page.md",
						"gamma://missing.md",
						"unknown-repo://page.md"
					],
					"""{ "index.md": {}, "page.md": {} }"""
				),
				["beta"] = Repository(
					"beta",
					["alpha://index.md", "alpha://missing.md", "gamma://missing-too.md"],
					"""{ "page.md": { "anchors": [ "known" ] } }"""
				),
				["gamma"] = Repository("gamma", [], """{ "page.md": {} }""")
			}.ToFrozenDictionary(),
			LinkIndexEntries = new Dictionary<string, LinkRegistryEntry>
			{
				["alpha"] = Entry("alpha"),
				["beta"] = Entry("beta")
			}.ToFrozenDictionary()
		};

	private static LinkRegistryEntry Entry(string name) =>
		new() { Repository = name, Branch = "main", ETag = "etag", Path = $"elastic/{name}/main/links.json" };

	private static string Validate(string? linksTo, string? linksFrom)
	{
		var service = new LinkIndexService(NullLoggerFactory.Instance, new MockFileSystem());
		var filterType = typeof(LinkIndexService).GetNestedType("RepositoryFilter", BindingFlags.NonPublic)!;
		var filter = Activator.CreateInstance(filterType, nonPublic: true)!;
		filterType.GetProperty("LinksTo")!.SetValue(filter, linksTo);
		filterType.GetProperty("LinksFrom")!.SetValue(filter, linksFrom);
		var index = Index();
		var method = typeof(LinkIndexService).GetMethod("ValidateCrossLinks", BindingFlags.NonPublic | BindingFlags.Instance)!;
		var collector = new CapturingCollector();

		var result = (bool)method.Invoke(service, [collector, index, new CrossLinkResolver(index), filter])!;

		var sb = new StringBuilder();
		_ = sb.Append("passed: ").AppendLine(result ? "true" : "false");
		_ = sb.Append(collector.Diagnostics.ToSnapshotText());
		return sb.ToString();
	}

	[Test]
	public void Validate_AllRepositories_ReportsEveryBrokenCrossLink() => Validate(null, null).Should().MatchSnapshot();

	[Test]
	public void Validate_LinksToOneRepository_OnlyChecksInboundLinks() => Validate("beta", null).Should().MatchSnapshot();

	[Test]
	public void Validate_LinksToARepositoryNobodyLinksTo_PassesWithNoDiagnostics() =>
		Validate("gamma-unused", null).Should().MatchInlineSnapshot("""
			passed: true

			""");

	[Test]
	public void Validate_LinksFromOneRepository_OnlyChecksThatRepository() => Validate(null, "beta").Should().MatchSnapshot();

	[Test]
	public void Validate_LinksFromARepositoryWithoutCrossLinks_Passes() =>
		Validate(null, "gamma").Should().MatchInlineSnapshot("""
			passed: true

			""");

	[Test]
	public void Validate_LinksToAndFromTogether_LinksToWins() => Validate("alpha", "beta").Should().MatchSnapshot();
}

public class RedirectKvsDiffSnapshotTests
{
	private static string Describe(Dictionary<string, string> sourced, string[] existing)
	{
		var (toPut, toDelete) = RedirectKvsDiff.ComputeBatchUpdates(sourced, existing);
		var sb = new StringBuilder();
		foreach (var put in toPut)
			_ = sb.Append("put    ").Append(put.Key).Append(" -> ").AppendLine(put.Value);
		foreach (var delete in toDelete)
			_ = sb.Append("delete ").AppendLine(delete.Key);
		_ = sb.Append("would wipe all existing: ").AppendLine(RedirectKvsDiff.WouldWipeAllExisting(sourced, existing) ? "true" : "false");
		return sb.ToString();
	}

	[Test]
	public void ComputeBatchUpdates_MixedState_PutsEverySourcedKeyAndDeletesTheStaleOnes() =>
		Describe(
			new Dictionary<string, string>
			{
				["/docs/kept"] = "/docs/kept-new",
				["/docs/added"] = "/docs/added-new",
				["/docs/changed"] = "/docs/changed-again"
			},
			["/docs/kept", "/docs/changed", "/docs/stale", "/docs/another-stale"]
		)
			.Should()
			.MatchInlineSnapshot(
				"""
			put    /docs/kept -> /docs/kept-new
			put    /docs/added -> /docs/added-new
			put    /docs/changed -> /docs/changed-again
			delete /docs/stale
			delete /docs/another-stale
			would wipe all existing: false

			"""
			);

	[Test]
	public void ComputeBatchUpdates_EmptySourcedAgainstExisting_DeletesAllAndFlagsTheWipe() =>
		Describe([], ["/docs/a", "/docs/b"])
			.Should()
			.MatchInlineSnapshot("""
			delete /docs/a
			delete /docs/b
			would wipe all existing: true

			""");

	[Test]
	public void ComputeBatchUpdates_KeysDifferingByCaseOrSlash_AreDifferentKeys() =>
		Describe(new Dictionary<string, string> { ["/docs/Page"] = "/docs/x", ["/docs/page/"] = "/docs/y" }, ["/docs/page", "/docs/Page"])
			.Should()
			.MatchInlineSnapshot(
				"""
			put    /docs/Page -> /docs/x
			put    /docs/page/ -> /docs/y
			delete /docs/page
			would wipe all existing: false

			"""
			);

	[Test]
	public void ComputeBatchUpdates_DuplicateExistingKeys_AreDeletedOnceEach() =>
		Describe([], ["/docs/a", "/docs/a"])
			.Should()
			.MatchInlineSnapshot("""
			delete /docs/a
			would wipe all existing: true

			""");
}
