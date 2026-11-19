// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Publishing;

namespace Elastic.Documentation.Configuration.Tests.Loading;

public class AssemblyLoaderTests
{
	private sealed class FakeEnvironment(params (string Name, string Value)[] variables) : IEnvironmentVariables
	{
		private readonly Dictionary<string, string> _variables = variables.ToDictionary(v => v.Name, v => v.Value);

		public string? GetEnvironmentVariable(string name) => _variables.GetValueOrDefault(name);

		public bool IsRunningOnCI => _variables.ContainsKey("GITHUB_ACTIONS");
	}

	private const string Yaml =
		"references:\n  elasticsearch:\n    path: ../elasticsearch\n  kibana:\n    repo: https://example.test/kibana.git\n";

	private static AssemblyConfiguration Deserialize(string yaml, params (string Name, string Value)[] variables) =>
		AssemblyLoader.Deserialize(yaml, environment: new FakeEnvironment(variables));

	[Test]
	public void Deserialize_OutsideCi_DefaultsOriginToSsh() =>
		Deserialize(Yaml).ReferenceRepositories["elasticsearch"].Origin.Should().Be("git@github.com:elastic/elasticsearch.git");

	[Test]
	public void Deserialize_OnGitHubActionsWithToken_DefaultsOriginToTokenHttps() =>
		Deserialize(Yaml, ("GITHUB_ACTIONS", "true"), ("GITHUB_TOKEN", "t0k"))
			.ReferenceRepositories["elasticsearch"]
			.Origin
			.Should()
			.Be("https://oauth2:t0k@github.com/elastic/elasticsearch.git");

	[Test]
	public void Deserialize_OnGitHubActionsWithoutToken_DefaultsOriginToPlainHttps() =>
		Deserialize(Yaml, ("GITHUB_ACTIONS", "true"))
			.ReferenceRepositories["elasticsearch"]
			.Origin
			.Should()
			.Be("https://github.com/elastic/elasticsearch.git");

	[Test]
	public void Deserialize_ExplicitRepo_IsKeptInCi() =>
		Deserialize(Yaml, ("GITHUB_ACTIONS", "true")).ReferenceRepositories["kibana"].Origin.Should().Be("https://example.test/kibana.git");

	[Test]
	public void Deserialize_InCi_DropsLocalPathOverrides()
	{
		Deserialize(Yaml).ReferenceRepositories["elasticsearch"].Path.Should().Be("../elasticsearch");
		Deserialize(Yaml, ("CI", "true")).ReferenceRepositories["elasticsearch"].Path.Should().BeNull();
	}

	[Test]
	public void Deserialize_ReferenceNamedLikeTheNarrative_ThrowsTheDuplicateKeyExceptionAsItAlwaysHas()
	{
		var act = () => Deserialize("references:\n  docs-content:\n");

		_ = act.Should().Throw<ArgumentException>().WithMessage("An item with the same key has already been added. Key: docs-content*");
	}

	[Test]
	public void Deserialize_SkippedReferenceNamedLikeTheNarrative_Loads() =>
		Deserialize("references:\n  docs-content:\n    skip: true\n").AvailableRepositories.Keys.Should().Equal("docs-content");

	[Test]
	public void Deserialize_EnvironmentWithoutBody_ThrowsNullReferenceAsItAlwaysHas()
	{
		var act = () => Deserialize("environments:\n  prod:\n");

		_ = act.Should().Throw<NullReferenceException>();
	}

	[Test]
	public void Deserialize_GoogleTagManagerEnabledBeforeItsId_ThrowsFromTheKeyThatIsRead() =>
		_ = ((Action)(() => Deserialize("environments:\n  prod:\n    google_tag_manager:\n      enabled: true\n      id: ''\n")))
			.Should()
			.Throw<Exception>()
			.WithInnerException<ArgumentException>()
			.WithMessage("Id is required when Enabled is true.");

	[Test]
	public void Deserialize_GoogleTagManagerIdBeforeEnabled_Loads() =>
		Deserialize("environments:\n  prod:\n    google_tag_manager:\n      id: ''\n      enabled: true\n")
			.Environments["prod"]
			.GoogleTagManager
			.Enabled
			.Should()
			.BeTrue();

	[Test]
	public void Deserialize_EmptySharedConfiguration_LoadsAsItAlwaysHas() =>
		Deserialize("shared_configuration:\n").SharedConfigurations.Should().BeEmpty();

	[Test]
	[Arguments("google_tag_manager")]
	[Arguments("optimizely")]
	public void Deserialize_EmptyOptionalEnvironmentSection_LoadsAndOnlyFailsWhereTheEnvironmentIsRead(string section)
	{
		var config = Deserialize($"environments:\n  prod:\n    {section}:\n");

		config.Environments.Keys.Should().Equal("prod");
		var read =
			() => section == "optimizely"
				? config.Environments["prod"].Optimizely.Enabled
				: config.Environments["prod"].GoogleTagManager.Enabled;
		_ = read.Should().Throw<NullReferenceException>();
	}

	[Test]
	public void Load_MalformedYaml_IsAnErrorFindingWithTheParsersMessage()
	{
		var result = AssemblyLoader.Yaml(false, new FakeEnvironment()).Load("references: [not, a, map]", "assembler.yml");

		result.ValueOrNull().Should().BeNull();
		result.Findings().Should().ContainSingle(d => d.Severity == Severity.Error && d.File == "assembler.yml");
	}

	[Test]
	public void Load_ValidYaml_ReportsNothing() =>
		AssemblyLoader.Yaml(false, new FakeEnvironment()).Load(Yaml, "assembler.yml").Findings().Should().BeEmpty();

	[Test]
	public void Load_SkippingPrivateRepositories_ListsThemOnlyByName()
	{
		var config = Deserialize("references:\n  open:\n  closed:\n    private: true\n");
		var skipping = AssemblyLoader.Deserialize(
			"references:\n  open:\n  closed:\n    private: true\n",
			skipPrivateRepositories: true,
			environment: new FakeEnvironment()
		);

		config.PrivateRepositories.Keys.Should().Equal("closed");
		skipping.ReferenceRepositories.Keys.Should().Equal("open");
		skipping.PrivateRepositories.Keys.Should().Equal("closed");
		skipping.AllPrivateRepositoryNames.Should().BeEquivalentTo(["closed"]);
	}

	[Test]
	public void Load_BaselineAssembler_SatisfiesTheAssemblyRules()
	{
		foreach (var skip in new[] { false, true })
		{
			using var provider = Golden.GoldenConfig.CreateProvider(skipPrivateRepositories: skip);
			var config = AssemblyLoader.Create(provider, new FakeEnvironment());

			AssemblyConfiguration.Constraints.Report(config).Violations.Should().BeEmpty();
		}
	}

	// Every key of the file is read by a DTO member that has a member of the same name on the record: a key added to the file or the model alone fails here.
	[Test]
	[Arguments(typeof(RepositoryDto), typeof(Repository))]
	[Arguments(typeof(PublishEnvironmentDto), typeof(PublishEnvironment))]
	[Arguments(typeof(GoogleTagManagerDto), typeof(GoogleTagManager))]
	[Arguments(typeof(OptimizelyDto), typeof(Optimizely))]
	public void Dto_EveryMemberIsProjectedOntoTheRecord(Type dto, Type record)
	{
		var recordMembers = PublicProperties(record).ToHashSet();

		PublicProperties(dto).Where(name => !recordMembers.Contains(name)).Should().BeEmpty();
		recordMembers.Except(PublicProperties(dto)).Except(["Name", "EqualityContract"]).Should().BeEmpty();
	}

	[Test]
	public void Dto_TheNarrativeAlsoReadsAnIgnoredNameKey() =>
		PublicProperties(typeof(NarrativeRepositoryDto)).Except(PublicProperties(typeof(RepositoryDto))).Should().Equal("Name");

	private static IEnumerable<string> PublicProperties(Type type) =>
		type
			.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
			.Concat(type.BaseType is { } b && b != typeof(object) && !type.IsSealed ? [] : [])
			.Select(p => p.Name);
}
