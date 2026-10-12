// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Codex;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.Versions;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class RelatedLearningConstraintsTests
{
	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		DomainAutoTest.For(RelatedLearningConstraints.All, DomainGen.Of<RelatedLearningCatalog>()).CounterExamples(CounterExamples).Axes();

	// each case adds one link that is wrong in one way (under an id no generated link has), so it holds whatever links were generated
	private static RelatedLearningCatalog Plus(RelatedLearningCatalog c, RelatedLearningLink link, string? key = null) =>
		new() { Links = new Dictionary<string, RelatedLearningLink>(c.Links) { [key ?? link.Id] = link }.ToFrozenDictionary() };

	private static RelatedLearningLink Link => new() { Id = "intro", Title = "Introduction", Url = "https://www.elastic.co/intro" };

	private static IReadOnlyDictionary<string, Func<RelatedLearningCatalog, RelatedLearningCatalog>> CounterExamples =>
		new Dictionary<string, Func<RelatedLearningCatalog, RelatedLearningCatalog>> { };
}

public class AssemblyConstraintsTests
{
	private static readonly string[] RuntimeEnvironments = ["dev", "edge", "staging", "prod"];

	private static string Segment(string key) => key.Replace('/', '-').Replace('\\', '-').Replace("..", "--", StringComparison.Ordinal);

	/// <summary>
	/// An assembler.yml as the loader derives it: repositories keyed by their own name, the available ones the published references plus the narrative, the private ones
	/// a subset of the references that is also listed by name, environments named as the runtime knows them, with the options each one enables filled in.
	/// </summary>
	internal static AutoOptions Configure(AutoOptions options) =>
		options
			.Normalize<Repository>(
				r =>
					r with
					{
						Skip = false,
						SparsePaths = r.SparsePaths.Length > 0 && !r.SparsePaths.Any(string.IsNullOrWhiteSpace) ? r.SparsePaths : ["docs"],
						CloneTimeout = r.CloneTimeout is { } t && t > TimeSpan.Zero ? t : null
					}
			)
			.Normalize<NarrativeRepository>(
				n =>
					n with
					{
						Name = NarrativeRepository.RepositoryName,
						Skip = false,
						SparsePaths = n.SparsePaths.Length > 0 && !n.SparsePaths.Any(string.IsNullOrWhiteSpace) ? n.SparsePaths : ["docs"],
						CloneTimeout = null
					}
			)
			.Normalize<PublishEnvironment>(
				e =>
					e with
					{
						Uri = "https://www.elastic.test",
						PathPrefix = e.PathPrefix?.Replace("${", "", StringComparison.Ordinal),
						GoogleTagManager = e.GoogleTagManager.Enabled && string.IsNullOrEmpty(e.GoogleTagManager.Id)
							? e.GoogleTagManager with { Id = "GTM-1" }
							: e.GoogleTagManager,
						Optimizely = e.Optimizely.Enabled && string.IsNullOrWhiteSpace(e.Optimizely.Id)
							? e.Optimizely with { Id = "1" }
							: e.Optimizely,
						FeatureFlags = e
							.FeatureFlags
							.GroupBy(f => f.Key.ToLowerInvariant().Replace('_', '-'))
							.ToDictionary(g => g.Key, g => g.First().Value),
						WebsiteSearchScriptUrl = e.WebsiteSearchScriptUrl is { } url && Uri.TryCreate(url, UriKind.Absolute, out _)
							? url
							: null
					}
			)
			.Normalize<AssemblyConfiguration>(a =>
			{
				var narrative = a.Narrative;
				var reference = a
					.ReferenceRepositories
					.Select(kv => (Key: Segment(kv.Key), kv.Value))
					.Where(kv => kv.Key.Length > 0 && kv.Key != NarrativeRepository.RepositoryName)
					.GroupBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
					.Select(g => g.First())
					.ToDictionary(kv => kv.Key, kv => kv.Value with { Name = kv.Key });
				var isPrivate = reference.Where(kv => kv.Value.Private).ToDictionary(kv => kv.Key, kv => kv.Value);
				return a with
				{
					ReferenceRepositories = reference,
					PrivateRepositories = isPrivate,
					AllPrivateRepositoryNames = new HashSet<string>(isPrivate.Keys),
					Environments = a
						.Environments
						.Values
						.Take(RuntimeEnvironments.Length)
						.Select((e, i) => (Name: RuntimeEnvironments[i], Environment: e))
						.ToDictionary(x => x.Name, x => x.Environment with { Name = x.Name })
				};
			});

	private static Gen<AssemblyConfiguration> Generated() => DomainGen.Of<AssemblyConfiguration>(o => Configure(o));

	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		DomainAutoTest.For(AssemblyConstraints.All, Generated()).CounterExamples(CounterExamples).Axes();

	private static PublishEnvironment Prod =>
		new()
		{
			Name = "prod",
			Uri = "https://www.elastic.co",
			ContentSource = ContentSource.Current,
			GoogleTagManager = new() { Enabled = true, Id = "GTM-1" },
			Optimizely = new() { Enabled = true, Id = "1" }
		};

	// Each case adds the one repository or environment that is wrong in one way to what was generated, under a name no generated one has (the generator
	// names repositories with words and the environments after the runtime list, so "prod" is the one a case may replace).
	private static IReadOnlyDictionary<string, Func<AssemblyConfiguration, AssemblyConfiguration>> CounterExamples =>
		new Dictionary<string, Func<AssemblyConfiguration, AssemblyConfiguration>>
		{
			["narrative-name-reserved"] =
				a => WithReference(a, NarrativeRepository.RepositoryName, new Repository { Name = NarrativeRepository.RepositoryName }),
			["environment-name-in-runtime-list"] = a => WithEnvironment(a, "preview", Prod with { Name = "preview" }),
			["feature-flag-keys-normalized"] =
				a => WithProd(a, e => e with { FeatureFlags = new Dictionary<string, bool> { ["PAGE_FEEDBACK"] = true } }),
		};

	private static AssemblyConfiguration WithReference(AssemblyConfiguration a, string key, Repository repository) =>
		a with { ReferenceRepositories = new Dictionary<string, Repository>(a.ReferenceRepositories) { [key] = repository } };

	private static AssemblyConfiguration WithPrivate(AssemblyConfiguration a, string key, Repository repository, bool listed = true) =>
		a with
		{
			PrivateRepositories = new Dictionary<string, Repository>(a.PrivateRepositories) { [key] = repository },
			AllPrivateRepositoryNames = listed ? new HashSet<string>(a.AllPrivateRepositoryNames) { key } : a.AllPrivateRepositoryNames
		};

	private static AssemblyConfiguration WithEnvironment(AssemblyConfiguration a, string key, PublishEnvironment environment) =>
		a with { Environments = new Dictionary<string, PublishEnvironment>(a.Environments) { [key] = environment } };

	private static AssemblyConfiguration WithProd(AssemblyConfiguration a, Func<PublishEnvironment, PublishEnvironment> change) =>
		WithEnvironment(a, "prod", change(Prod));
}

public class SiteNavigationConstraintsTests
{
	/// <summary>
	/// A navigation.yml as the loader hands it over: every mount a repository of its own (so sources, prefixes and repository prefixes are unique and none is a text prefix of another), under a legal
	/// path, with a boolean island; sections titled once, each external or holding mounts or a dropdown, not several; dropdown links complete and relative to the site; phantoms nothing mounts.
	/// </summary>
	internal static SiteNavigationDefinition Valid(SiteNavigationDefinition n)
	{
		var next = 0;

		SiteTableOfContentsRef Mount(SiteTableOfContentsRef t)
		{
			var i = next++;
			return t with
			{
				Source = new Uri($"repo{i}://toc"),
				PathPrefix = $"reference/p{i}",
				ShorthandPath = null,
				UnknownKeys = [],
				IslandRaw = t.Island ? "true" : null,
				Children = [.. t.Children.Select(Mount)]
			};
		}

		var entries = new List<SiteNavigationEntry>();
		foreach (var entry in n.TableOfContents)
		{
			if (entry.Value is SiteTableOfContentsRef toc)
			{
				entries.Add(new SiteNavigationEntry(Mount(toc)));
			}
			else if (entry.Value is SiteSectionRef section)
			{
				var title = $"Section {entries.Count}";
				var children = section.ExternalUrl is null ? section.Children.Select(Mount).ToList() : [];
				var links = section.ExternalUrl is null
					? section
						.DropdownLinks
						.Select((l, k) => new SiteDropdownLinkRef($"Link {k}", $"solutions/s{entries.Count}-{k}"))
						.ToList()
					: [];
				var external = section.ExternalUrl is not null || (children.Count == 0 && links.Count == 0)
					? $"https://example.test/{entries.Count}"
					: null;
				entries.Add(new SiteNavigationEntry(new SiteSectionRef(title, external, children, links) with
				{
					UnknownKeys = [],
					Position = null
				}));
			}
		}
		return n with
		{
			TableOfContents = entries,
			Phantoms = [.. n.Phantoms.Select((_, j) => new PhantomRegistration { Source = $"phantom{j}://toc" })]
		};
	}

	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		DomainAutoTest
			.For(SiteNavigationConstraints.All, DomainGen.Of<SiteNavigationDefinition>(o => o.Normalize<SiteNavigationDefinition>(Valid)))
			.CounterExamples(CounterExamples)
			.Axes();

	private static SiteTableOfContentsRef Mount(string source, string pathPrefix) => new(new Uri(source), pathPrefix, []);

	private static SiteNavigationDefinition Plus(SiteNavigationDefinition navigation, params SiteTableOfContentsRef[] mounts) =>
		navigation with { TableOfContents = [.. navigation.TableOfContents, .. mounts.Select(m => new SiteNavigationEntry(m))] };

	private static SiteNavigationDefinition Plus(SiteNavigationDefinition navigation, params SiteSectionRef[] sections) =>
		navigation with { TableOfContents = [.. navigation.TableOfContents, .. sections.Select(s => new SiteNavigationEntry(s))] };

	// each case adds the mounts or sections that are wrong in one way; their sources and prefixes are ones the generated navigation never uses
	private static IReadOnlyDictionary<string, Func<SiteNavigationDefinition, SiteNavigationDefinition>> CounterExamples =>
		new Dictionary<string, Func<SiteNavigationDefinition, SiteNavigationDefinition>>
		{
			["section-external-exclusive"] =
				n => Plus(
					n,
					new SiteSectionRef("Elsewhere", "https://example.test", [Mount("logstash://reference", "reference/logstash")], [])
				),
			["section-not-empty"] = n => Plus(n, new SiteSectionRef("Empty", null, [], [])),
			["dropdown-link-complete"] =
				n => Plus(n, new SiteSectionRef("Links", null, [], [new SiteDropdownLinkRef(" ", "solutions/search")])),
			["dropdown-link-url-valid"] =
				n => Plus(n, new SiteSectionRef("Links", null, [], [new SiteDropdownLinkRef("Docs", "https://example.test/docs")])),
			["phantom-not-mounted"] =
				n =>
					Plus(n, Mount("mounted://reference", "reference/mounted")) with
					{
						Phantoms = [.. n.Phantoms, new PhantomRegistration { Source = "mounted://reference" }]
					},
			["mount-sources-segment-prefix-free"] =
				n => Plus(n, Mount("pf://ref", "reference/pf"), Mount("pf://ref-foo", "reference/pf-foo")),
			["mount-is-published"] = n => Plus(n, Mount("eland://sphinx", "reference/eland")),
			["entry-unknown-keys"] =
				n => Plus(n, Mount("logstash://reference", "reference/logstash") with { UnknownKeys = ["pathprefix"] }),
			["mount-repo-key"] = n => Plus(n, Mount("logstash://reference", "reference/logstash") with { UnknownKeys = ["repo"] }),
			["island-is-boolean"] = n => Plus(n, Mount("logstash://reference", "reference/logstash") with { IslandRaw = "yes" }),
			["mount-has-path-prefix"] = n => Plus(n, Mount("np://reference", "")),
			["path-prefix-is-a-url-path"] = n => Plus(n, Mount("sp://reference", "a prefix with spaces")),
			["path-prefixes-unique"] =
				n => Plus(n, Mount("pa://reference", "reference/shared"), Mount("pb://reference", "reference/shared")),
		};
}

public class VersioningSystemConstraintsTests
{
	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		DomainAutoTest.For(VersioningSystemConstraints.All, DomainGen.Of<VersioningSystemCatalog>()).CounterExamples(CounterExamples).Axes();

	[Test]
	public void KeyMatchesId_NamesTheKeyAndTheIdOfTheSystemFoundThere() =>
		DomainGen.Of<VersioningSystemCatalog>().Sample(
			catalog =>
			{
				var rule = VersioningSystemConstraints.VersioningSystemKeyMatchesId;
				VersioningSystemConstraints.All.Report(catalog).Of(rule).Should().BeEmpty();
				var broken = CounterExamples["versioning-system-key-matches-id"](catalog);
				var violation = VersioningSystemConstraints.All.Report(broken).Of(rule).Should().ContainSingle().Subject;
				violation.Message.Should().Contain("'stack'").And.Contain("'ecs'");
			},
			iter: 20
		);

	// the stack entry is the one the examples vary; every other id stays defined, so only the rule under test is broken
	private static VersioningSystemCatalog WithStack(VersioningSystemCatalog c, string @base, string current) =>
		new()
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>(c.VersioningSystems)
			{
				[VersioningSystemId.Stack] = new VersioningSystem
				{
					Id = VersioningSystemId.Stack,
					Base = (SemVersion)@base,
					Current = (SemVersion)current
				}
			}
		};

	private static VersioningSystemCatalog WithStackSystem(VersioningSystemCatalog c, VersioningSystem system) =>
		new()
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>(c.VersioningSystems)
			{
				[VersioningSystemId.Stack] = system
			}
		};

	private static IReadOnlyDictionary<string, Func<VersioningSystemCatalog, VersioningSystemCatalog>> CounterExamples =>
		new Dictionary<string, Func<VersioningSystemCatalog, VersioningSystemCatalog>>
		{
			["versioning-system-key-matches-id"] =
				c => new VersioningSystemCatalog
				{
					VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>(c.VersioningSystems)
					{
						[VersioningSystemId.Stack] = c.VersioningSystems[VersioningSystemId.Ecs]
					}
				}
		};
}

public class CodexPublishingConstraintsTests
{
	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		DomainAutoTest
			.For(CodexPublishingConstraints.All, DomainGen.Of<CodexConfiguration>())
			.CounterExamples(new Dictionary<string, Func<CodexConfiguration, CodexConfiguration>> { })
			.Axes();
}

public class CodexConfigurationConstraintsTests
{
	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		DomainAutoTest
			.For(
				CodexConfigurationConstraints.All,
				DomainGen.Of<CodexConfiguration>(
					o => o.Identity<CodexGroupDefinition, string>(g => g.Id).Normalize<CodexConfiguration>(
						c =>
							c with
							{
								CanonicalBaseUrl = c.CanonicalBaseUrl is { } u && Uri.TryCreate(u, UriKind.Absolute, out _) ? u : null,
								Groups = [.. c.Groups.GroupBy(g => g.Id, StringComparer.OrdinalIgnoreCase).Select(g => g.First())]
							}
					)
				)
			)
			.CounterExamples(CounterExamples)
			.Axes();

	private static CodexGroupDefinition Group(string id, string name) => new() { Id = id, Name = name };

	private static IReadOnlyDictionary<string, Func<CodexConfiguration, CodexConfiguration>> CounterExamples =>
		new Dictionary<string, Func<CodexConfiguration, CodexConfiguration>> { };
}
