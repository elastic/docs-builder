// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.Testing;
using Nullean.Entities.CsCheck;
using Nullean.Snapshots;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// The laws of an <see cref="AssemblyConfiguration"/> as the loader derives it: repositories keyed by their own name, the private ones listed by name. Each guards a configuration built by hand; the loader satisfies them by construction.
/// </summary>
public class AssemblyConstraintsTests
{
	private static string Segment(string key) => key.Replace('/', '-').Replace('\\', '-');

	/// <summary>An assembler.yml as the loader derives it: references keyed by their own name, the private ones listed by name.</summary>
	private static AssemblyConfiguration Normalized(AssemblyConfiguration a)
	{
		var reference = a
			.ReferenceRepositories
			.Select(kv => (Key: Segment(kv.Key), kv.Value))
			.Where(kv => kv.Key.Length > 0 && kv.Key != NarrativeRepository.RepositoryName)
			.GroupBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
			.Select(g => g.First())
			.ToDictionary(kv => kv.Key, kv => kv.Value with { Name = kv.Key, Skip = false });
		var isPrivate = reference.Where(kv => kv.Value.Private).ToDictionary(kv => kv.Key, kv => kv.Value);
		return a with
		{
			ReferenceRepositories = reference,
			PrivateRepositories = isPrivate,
			AllPrivateRepositoryNames = new HashSet<string>(isPrivate.Keys, StringComparer.OrdinalIgnoreCase)
		};
	}

	[Test]
	[MethodDataSource(nameof(Axes))]
	public Task EveryDeclaredAxisHolds(AxisCase axis) => axis.RunAsync();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest
			.For(
				AssemblyConstraints.All,
				GenAuto.Of<AssemblyConfiguration>(AutoMode.Plausible, o => o.Normalize<AssemblyConfiguration>(Normalized))
			)
			.Seed(RunSeed.Value)
			.CounterExamples(CounterExamples)
			.AsDataSource();

	private static AssemblyConfiguration WithReference(AssemblyConfiguration a, string key, Repository repository) =>
		a with { ReferenceRepositories = new Dictionary<string, Repository>(a.ReferenceRepositories) { [key] = repository } };

	// a repository that publishes docs, so that a private entry of the same name is an available one
	private static AssemblyConfiguration Published(AssemblyConfiguration a, string key) =>
		WithReference(a, key, new Repository { Name = key });

	private static AssemblyConfiguration WithPrivate(AssemblyConfiguration a, string key, Repository repository, bool listed = true) =>
		a with
		{
			PrivateRepositories = new Dictionary<string, Repository>(a.PrivateRepositories) { [key] = repository },
			AllPrivateRepositoryNames = listed ? new HashSet<string>(a.AllPrivateRepositoryNames) { key } : a.AllPrivateRepositoryNames
		};

	// each case adds one repository, under a name no generated one has, that is wrong in one way
	private static IReadOnlyDictionary<string, Func<AssemblyConfiguration, AssemblyConfiguration>> CounterExamples =>
		new Dictionary<string, Func<AssemblyConfiguration, AssemblyConfiguration>>
		{
			["repository-name-matches-key"] = a => WithReference(a, "keyed-here", new Repository { Name = "named-there" }),
			["private-keys-match-names"] =
				a => WithPrivate(Published(a, "keyed-here"), "keyed-here", new Repository { Name = "named-there", Private = true }),
			["private-is-listed"] =
				a => WithPrivate(Published(a, "unnamed"), "unnamed", new Repository { Name = "unnamed", Private = true }, listed: false)
		};

	private static AssemblyConfiguration Configuration(
		Dictionary<string, Repository> references,
		Dictionary<string, Repository> privateRepositories,
		params string[] privateNames
	) =>
		new()
		{
			Narrative = new NarrativeRepository(),
			ReferenceRepositories = references,
			PrivateRepositories = privateRepositories,
			AllPrivateRepositoryNames = new HashSet<string>(privateNames, StringComparer.OrdinalIgnoreCase),
			Environments = new Dictionary<string, PublishEnvironment>(),
			SharedConfigurations = new Dictionary<string, Repository>()
		};

	[Test]
	public void Configuration_RepositoryListedUnderAnotherName_ReportsTheRuleThatIsBroken() =>
		AssemblyConfiguration
			.Constraints
			.Report(Configuration(new Dictionary<string, Repository> { ["kibana"] = new() { Name = "kibana-docs" } }, []))
			.ToDiagnostics("assembler.yml")
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot("warning assembler.yml a referenced repository is keyed by a name other than its own.");

	[Test]
	public void Configuration_PrivateRepositoryNotListedByName_ReportsTheRuleThatIsBroken() =>
		AssemblyConfiguration
			.Constraints
			.Report(Configuration(
				new Dictionary<string, Repository> { ["secret"] = new() { Name = "secret", Private = true } },
				new Dictionary<string, Repository> { ["secret"] = new() { Name = "secret", Private = true } }
			))
			.ToDiagnostics("assembler.yml")
			.Select(d => d.RuleId)
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be("private-is-listed");

	[Test]
	public void Configuration_ConsistentSets_HaveNoViolation() =>
		AssemblyConfiguration
			.Constraints
			.Report(
				Configuration(
					new Dictionary<string, Repository> { ["secret"] = new() { Name = "secret", Private = true } },
					new Dictionary<string, Repository> { ["secret"] = new() { Name = "secret", Private = true } },
					"SECRET"
				)
			)
			.Violations
			.Should()
			.BeEmpty();
}

/// <summary>The laws of the operations on a <see cref="Repository"/>, verified against every rule it declares.</summary>
public class RepositoryConstraintsTests
{
	[Test]
	[MethodDataSource(nameof(Axes))]
	public Task EveryDeclaredAxisHolds(AxisCase axis) => axis.RunAsync();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest.For(RepositoryConstraints.All, GenAuto.Of<Repository>(AutoMode.Plausible)).Seed(RunSeed.Value).AsDataSource();

	[Test]
	public void AsConfigurationCheckout_ChecksOutOnlyTheConfigFolderAndKeepsTheRest()
	{
		var repository = new Repository
		{
			Name = "docs-builder",
			Origin = "git@example.test:org/docs.git",
			GitReferenceNext = "next",
			Private = true
		};

		var checkout = repository.AsConfigurationCheckout();

		checkout.SparsePaths.Should().Equal("config");
		checkout.Should().BeEquivalentTo(repository with { SparsePaths = ["config"] });
		repository.SparsePaths.Should().Equal("docs");
	}
}
