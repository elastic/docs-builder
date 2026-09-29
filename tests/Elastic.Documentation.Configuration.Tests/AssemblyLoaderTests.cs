// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
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
	public void Deserialize_ReferencesNotAMap_Throws()
	{
		var invalid = () => AssemblyLoader.Deserialize("references: [not, a, map]", environment: new Environment());
		_ = invalid.Should().Throw<Exception>();
	}

	// YamlDotNet names the bound type in the error for an unknown key. These pin the names users have always seen.
	[Test]
	[Arguments("bogus: 1", "Elastic.Documentation.Configuration.Assembler.AssemblyConfiguration")]
	[Arguments("references:\n  kibana:\n    bogus: 1", "Elastic.Documentation.Configuration.Assembler.Repository")]
	[Arguments("narrative:\n  bogus: 1", "Elastic.Documentation.Configuration.Assembler.NarrativeRepository")]
	[Arguments("environments:\n  prod:\n    bogus: 1", "Elastic.Documentation.Configuration.Assembler.PublishEnvironment")]
	public void Deserialize_UnknownKey_NamesTheHistoricalType(string yaml, string typeName)
	{
		var act = () => AssemblyLoader.Deserialize(yaml, environment: new Environment());
		_ = act.Should().Throw<Exception>().WithMessage($"*'bogus' not found on type '{typeName}'*");
	}

	[Test]
	[Arguments("bogus: 1", "Elastic.Documentation.Configuration.Toc.SiteNavigationFile")]
	[Arguments("phantoms:\n  - bogus: 1", "Elastic.Documentation.Configuration.Toc.PhantomRegistration")]
	public void SiteNavigation_UnknownKey_NamesTheHistoricalType(string yaml, string typeName)
	{
		var act = () => Toc.SiteNavigationLoader.Deserialize(yaml);
		_ = act.Should().Throw<Exception>().WithMessage($"*'bogus' not found on type '{typeName}'*");
	}
}
