// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Codex;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Configuration.Tests;

public class CodexSiteLoaderTests
{
	private const string Full =
		"""
		site_prefix: " docs/ "
		environment: " INTERNAL "
		title: Docs
		canonical_base_url: https://codex.example.org
		groups:
		  - id: g1
		    name: Group one
		    description: First
		    icon: kibana
		""";

	private static CodexConfiguration Read(string yaml) => CodexSiteLoader.Load(yaml, "codex.yml").OrThrow();

	[Test]
	public void Load_EveryKey_ReachesTheRecord()
	{
		var codex = Read(Full);

		codex.SitePrefix.Should().Be("/docs");
		codex.Environment.Should().Be("internal");
		codex.Title.Should().Be("Docs");
		codex.CanonicalBaseUrl.Should().Be("https://codex.example.org");
		codex
			.Groups
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(new CodexGroupDefinition { Id = "g1", Name = "Group one", Description = "First", Icon = "kibana" });
	}

	[Test]
	[Arguments("/")]
	[Arguments("")]
	public void Load_RootSitePrefix_HasNoSegment(string prefix) => Read($"site_prefix: \"{prefix}\"").SitePrefix.Should().BeEmpty();

	[Test]
	public void Load_NoEnvironment_ReadsWithoutOne() => Read("title: Docs").Environment.Should().BeNull();

	[Test]
	public void LoadForPublish_NoEnvironment_IsAnErrorWithTheEnvironmentMessage()
	{
		var result = CodexSiteLoader.LoadForPublish("title: Docs", "codex.yml");

		result.Value.Should().BeOfType<Unreadable<PublishedCodexConfiguration>>();
		result
			.Findings()
			.Should()
			.ContainSingle()
			.Which
			.Message
			.Should()
			.Be("Codex configuration must specify an 'environment' (e.g., 'internal', 'security').");
	}

	[Test]
	public void LoadForPublish_WithEnvironment_Loads() =>
		CodexSiteLoader.LoadForPublish("environment: internal", "codex.yml").OrThrow().Configuration.Environment.Should().Be("internal");

	[Test]
	public void Load_MalformedYaml_IsAnErrorThatNamesTheFile()
	{
		var result = CodexSiteLoader.Load("groups: [", "codex.yml");

		result.Value.Should().BeOfType<Unreadable<CodexConfiguration>>();
		result.Findings().Should().ContainSingle().Which.Message.Should().StartWith("Failed to read codex configuration 'codex.yml': ");
	}
}
