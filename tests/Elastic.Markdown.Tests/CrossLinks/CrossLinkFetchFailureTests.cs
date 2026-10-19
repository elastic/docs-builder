// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;

namespace Elastic.Markdown.Tests.CrossLinks;

public class CrossLinkFetchFailureTests(ITestOutputHelper output)
{
	[Fact]
	public void TryResolve_WhenFetchFailed_DoesNotEmitPerLinkError()
	{
		var crossLinks = BuildCrossLinksWithFetchFailure("synthetics-service", "Git clone failed: auth error");

		string? emittedError = null;
		var resolver = new IsolatedBuildEnvironmentUriResolver();
		var success = CrossLinkResolver.TryResolve(
			s => emittedError = s,
			crossLinks,
			resolver,
			new Uri("synthetics-service://index.md", UriKind.Absolute),
			out _
		);

		success.Should().BeFalse();
		emittedError.Should().BeNull();
	}

	[Fact]
	public void EmitFetchFailures_EmitsOneSummaryForTheConfigurationFile()
	{
		var collector = new TestDiagnosticsCollector(output);
		var crossLinks = BuildCrossLinksWithFetchFailure(
			"synthetics-service",
			"Git clone failed: fatal: could not read Username for 'https://github.com'");

		CrossLinkFetchDiagnostics.EmitFetchFailures(collector, "/docs/docset.yml", crossLinks);

		var diagnostic = collector.Diagnostics.Should().ContainSingle().Which;
		diagnostic.File.Should().Be("/docs/docset.yml");
		diagnostic.Message.Should().Contain("Could not fetch the Elastic Internal Docs link index from https://github.com/elastic/codex-link-index");
		diagnostic.Message.Should().Contain("Git clone failed:");
		diagnostic.Message.Should().Contain("Cross-links to synthetics-service were not validated.");
		diagnostic.Message.Should().NotContain("is not a valid link");
	}

	private static FetchedCrossLinks BuildCrossLinksWithFetchFailure(string repository, string failureReason)
	{
		var emptyRepositoryLinks = new RepositoryLinks
		{
			Links = [],
			Origin = new GitCheckoutInformation
			{
				Branch = "main",
				RepositoryName = repository,
				Remote = "origin",
				Ref = "refs/heads/main"
			},
			UrlPathPrefix = "",
			CrossLinks = []
		};

		return new FetchedCrossLinks
		{
			DeclaredRepositories = [repository],
			LinkReferences = new Dictionary<string, RepositoryLinks> { [repository] = emptyRepositoryLinks }.ToFrozenDictionary(),
			LinkIndexEntries = new Dictionary<string, LinkRegistryEntry>().ToFrozenDictionary(),
			RegistryUrlsByRepository = new Dictionary<string, string>
			{
				[repository] = "https://github.com/elastic/codex-link-index"
			}.ToFrozenDictionary(),
			RegistryByRepository = new Dictionary<string, DocSetRegistry>
			{
				[repository] = DocSetRegistry.Internal
			}.ToFrozenDictionary(),
			FetchFailures = new Dictionary<string, string> { [repository] = failureReason }.ToFrozenDictionary()
		};
	}
}
