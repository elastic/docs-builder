// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Publishing;
using static Elastic.Documentation.Configuration.Tests.Golden.Golden;

namespace Elastic.Documentation.Configuration.Tests;

public class AssemblyLoaderTests
{
	private sealed class Environment(params (string Name, string Value)[] variables) : IEnvironmentVariables
	{
		private readonly Dictionary<string, string> _variables = variables.ToDictionary(v => v.Name, v => v.Value);

		public string? GetEnvironmentVariable(string name) => _variables.GetValueOrDefault(name);

		public bool IsRunningOnCI => _variables.ContainsKey("GITHUB_ACTIONS");
	}

	private const string Yaml =
		"references:\n  elasticsearch:\n    path: ../elasticsearch\n  kibana:\n    repo: https://example.test/kibana.git\n";

	[Test]
	public void Deserialize_OutsideCi_DefaultsOriginToSsh() =>
		AssemblyLoader
			.Deserialize(Yaml, environment: new Environment())
			.ReferenceRepositories["elasticsearch"]
			.Origin
			.Should()
			.Be("git@github.com:elastic/elasticsearch.git");

	[Test]
	public void Deserialize_OnGitHubActionsWithToken_DefaultsOriginToTokenHttps() =>
		AssemblyLoader
			.Deserialize(Yaml, environment: new Environment(("GITHUB_ACTIONS", "true"), ("GITHUB_TOKEN", "t0k")))
			.ReferenceRepositories["elasticsearch"]
			.Origin
			.Should()
			.Be("https://oauth2:t0k@github.com/elastic/elasticsearch.git");

	[Test]
	public void Deserialize_ExplicitRepo_IsKeptInCi() =>
		AssemblyLoader
			.Deserialize(Yaml, environment: new Environment(("GITHUB_ACTIONS", "true")))
			.ReferenceRepositories["kibana"]
			.Origin
			.Should()
			.Be("https://example.test/kibana.git");

	[Test]
	public void Deserialize_InCi_DropsLocalPathOverrides()
	{
		AssemblyLoader
			.Deserialize(Yaml, environment: new Environment())
			.ReferenceRepositories["elasticsearch"]
			.Path
			.Should()
			.Be("../elasticsearch");
		AssemblyLoader
			.Deserialize(Yaml, environment: new Environment(("CI", "true")))
			.ReferenceRepositories["elasticsearch"]
			.Path
			.Should()
			.BeNull();
	}

	[Test]
	public void Load_BaselineAssembler_Succeeds() =>
		AssemblyLoader.Load(BaselineProvider(), new Environment()).HasErrors.Should().BeFalse();

	[Test]
	public void LoadYaml_ContentSourceOmitted_DefaultsToNextAndReportsAuthoredShape()
	{
		var result = AssemblyLoader.LoadYaml(
			"environments:\n  preview:\n    uri: https://example.test",
			"assembler.yml",
			environment: new Environment()
		);

		result.Value!.Environments["preview"].ContentSource.Should().Be(ContentSource.Next);
		result
			.Diagnostics
			.Should()
			.ContainSingle(
				d => d.Severity == Severity.Warning && d.File == "assembler.yml" && d.Message ==
					"Environment 'preview' does not declare content_source, so it publishes 'next'. Set content_source to current, next or edge."
			);
	}

	[Test]
	public void Deserialize_ReferencesNotAMap_Throws()
	{
		var invalid = () => AssemblyLoader.Deserialize("references: [not, a, map]", environment: new Environment());
		_ = invalid.Should().Throw<Exception>();
	}

	// The deserializer ignores unmatched properties (a released binary must not crash on a key a newer file adds), so an unknown key is not an error.
	[Test]
	[Arguments("bogus: 1")]
	[Arguments("references:\n  kibana:\n    bogus: 1")]
	[Arguments("narrative:\n  bogus: 1")]
	[Arguments("environments:\n  prod:\n    bogus: 1")]
	public void Deserialize_UnknownKey_IsIgnored(string yaml)
	{
		var act = () => AssemblyLoader.Deserialize(yaml, environment: new Environment());
		_ = act.Should().NotThrow();
	}

	[Test]
	[Arguments("bogus: 1")]
	[Arguments("phantoms:\n  - bogus: 1")]
	public void SiteNavigation_UnknownKey_IsIgnored(string yaml)
	{
		var act = () => Toc.SiteNavigationLoader.Deserialize(yaml);
		_ = act.Should().NotThrow();
	}

	private const string TwoBadRepositories = "references:\n  a/b:\n  c/d:\n";

	[Test]
	public void LoadYaml_TwoBadRepositories_ReportsBoth()
	{
		var result = AssemblyLoader.LoadYaml(TwoBadRepositories, "assembler.yml", environment: new Environment());

		result.Value.Should().BeNull();
		result
			.Diagnostics
			.Where(d => d.Severity == Severity.Error)
			.Select(d => (d.RuleId, d.File, d.Message.Split('\'')[1]))
			.Should()
			.BeEquivalentTo([
				("repository-name-single-segment", "assembler.yml", "a/b"),
				("repository-name-single-segment", "assembler.yml", "c/d")
			]);
	}

	[Test]
	public void OrThrow_TwoBadRepositories_ListsBothInTheMessageAndCarriesTheDiagnostics()
	{
		var act = () => AssemblyLoader.LoadYaml(TwoBadRepositories, "assembler.yml", environment: new Environment()).OrThrow();

		var exception = act.Should().Throw<ConfigurationException>().Which;
		exception.Message.Should().Contain("Repository name 'a/b'").And.Contain("Repository name 'c/d'");
		exception.Message.Split(System.Environment.NewLine).Should().HaveCount(2);
		exception.Diagnostics.Count(d => d.Severity == Severity.Error).Should().Be(2);
		exception.Lines.Should().Contain(l => l.Contains("[repository-name-single-segment]"));
		_ = act.Should().Throw<InvalidOperationException>();
	}

	[Test]
	public void Deserialize_TwoBadRepositories_ThrowsWithBothMessages()
	{
		var act = () => AssemblyLoader.Deserialize(TwoBadRepositories, environment: new Environment());
		_ = act.Should().Throw<InvalidOperationException>().WithMessage("*Repository name 'a/b'*Repository name 'c/d'*");
	}

	[Test]
	public void LoadOnce_SameProvider_ParsesOnce()
	{
		var provider = BaselineProvider();

		AssemblyLoader.LoadOnce(provider).Should().BeSameAs(AssemblyLoader.LoadOnce(provider));
		AssemblyLoader.LoadOnce(BaselineProvider()).Should().NotBeSameAs(AssemblyLoader.LoadOnce(provider));
	}
}
