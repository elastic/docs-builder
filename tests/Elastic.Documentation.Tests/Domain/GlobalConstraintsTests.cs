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
	public void Laws() => RelatedLearningConstraints.All.Laws(DomainGen.Of<RelatedLearningCatalog>(), CounterExamples);

	// each case adds one link that is wrong in one way (under an id no generated link has), so it holds whatever links were generated
	private static RelatedLearningCatalog Plus(RelatedLearningCatalog c, RelatedLearningLink link, string? key = null) =>
		new() { Links = new Dictionary<string, RelatedLearningLink>(c.Links) { [key ?? link.Id] = link }.ToFrozenDictionary() };

	private static RelatedLearningLink Link => new() { Id = "intro", Title = "Introduction", Url = "https://www.elastic.co/intro" };

	private static IReadOnlyDictionary<string, Func<RelatedLearningCatalog, RelatedLearningCatalog>> CounterExamples =>
		new Dictionary<string, Func<RelatedLearningCatalog, RelatedLearningCatalog>>
		{
			["links-required"] = c => c with { Links = null! },
			["links-id-required"] = c => Plus(c, Link with { Id = null! }, "intro"),
			["links-title-required"] = c => Plus(c, Link with { Title = null! }),
			["links-url-required"] = c => Plus(c, Link with { Url = null! }),
			["link-has-title"] = c => Plus(c, Link with { Title = "" }),
			["link-has-url"] = c => Plus(c, Link with { Url = " " }),
			["link-url-absolute-http"] = c => Plus(c, Link with { Url = "ftp://example.test/intro" })
		};
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
	public void Laws() => AssemblyConstraints.All.Laws(Generated(), CounterExamples);

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
			["narrative-required"] = a => a with { Narrative = null! },
			["reference-repositories-required"] = a => a with { ReferenceRepositories = null! },
			["private-repositories-required"] = a => a with { PrivateRepositories = null! },
			["all-private-repository-names-required"] = a => a with { AllPrivateRepositoryNames = null! },
			["environments-required"] = a => a with { Environments = null! },
			["narrative-name-reserved"] =
				a => WithReference(a, NarrativeRepository.RepositoryName, new Repository { Name = NarrativeRepository.RepositoryName }),
			["google-tag-manager-has-id"] = a => WithProd(a, e => e with { GoogleTagManager = new() { Enabled = true } }),
			["environment-uri-absolute"] = a => WithProd(a, e => e with { Uri = "www.elastic.co" }),
			["optimizely-has-id"] = a => WithProd(a, e => e with { Optimizely = new() { Enabled = true } }),
			["environment-path-prefix-resolved"] = a => WithProd(a, e => e with { PathPrefix = "${ASSEMBLER_PREVIEW_PATH_PREFIX}" }),
			["repository-name-matches-key"] = a => WithReference(a, "kibana", new Repository { Name = "beats" }),
			["private-keys-match-names"] =
				a => WithPrivate(
					WithReference(a, "kibana", new Repository { Name = "kibana" }),
					"kibana",
					new Repository { Name = "beats", Private = true }
				),
			["narrative-not-skipped"] = a => a with { Narrative = new NarrativeRepository { Skip = true } },
			["repository-sparse-paths-valid"] = a => WithReference(a, "kibana", new Repository { Name = "kibana", SparsePaths = [" "] }),
			["repository-clone-timeout-positive"] =
				a => WithReference(a, "kibana", new Repository { Name = "kibana", CloneTimeout = TimeSpan.Zero }),
			["repository-skipped"] = a => WithReference(a, "kibana", new Repository { Name = "kibana", Skip = true }),
			["repository-names-unique-ignoring-case"] =
				a => WithReference(
					WithReference(a, "kibana", new Repository { Name = "kibana" }),
					"Kibana",
					new Repository { Name = "Kibana" }
				),
			["private-is-available"] = a => WithPrivate(a, "beats", new Repository { Name = "beats", Private = true }, listed: true),
			["private-is-listed"] =
				a => WithPrivate(
					WithReference(a, "kibana", new Repository { Name = "kibana" }),
					"kibana",
					new Repository { Name = "kibana", Private = true },
					listed: false
				),
			["environment-name-in-runtime-list"] = a => WithEnvironment(a, "preview", Prod with { Name = "preview" }),
			["feature-flag-keys-normalized"] =
				a => WithProd(a, e => e with { FeatureFlags = new Dictionary<string, bool> { ["PAGE_FEEDBACK"] = true } }),
			["environment-website-search-url-absolute"] = a => WithProd(a, e => e with { WebsiteSearchScriptUrl = "search/script.js" }),
			["repository-name-single-segment"] = a => a with { Narrative = new NarrativeRepository { Name = "docs/content" } },
			// The key stays in the runtime list; only the environment's own name is unusable in a store name.
			["environment-name-store-safe"] = a => WithProd(a, e => e with { Name = "pro d" }),
			["environment-name-lowercase"] = a => WithProd(a, e => e with { Name = "Prod" })
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
	public void Laws() =>
		SiteNavigationConstraints.All.Laws(
			DomainGen.Of<SiteNavigationDefinition>(o => o.Normalize<SiteNavigationDefinition>(Valid)),
			CounterExamples
		);

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
			["mount-source-unique"] =
				n => Plus(n, Mount("dup://reference", "reference/dup-a"), Mount("dup://reference", "reference/dup-b")),
			["section-titles-unique"] =
				n => Plus(n, new SiteSectionRef("Alpha", "https://a.test", [], []), new SiteSectionRef("alpha", "https://b.test", [], [])),
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
			["repository-path-prefixes-unique"] =
				n => Plus(n, Mount("rp://reference", "reference/rp"), Mount("rp://other", "reference/rp/"))
		};
}

public class VersioningSystemConstraintsTests
{
	[Test]
	public void Laws() => VersioningSystemConstraints.All.Laws(DomainGen.Of<VersioningSystemCatalog>(), CounterExamples);

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
			["versioning-systems-required"] = c => c with { VersioningSystems = null! },
			["versioning-systems-base-required"] =
				c => WithStackSystem(c, new VersioningSystem { Id = VersioningSystemId.Stack, Base = null!, Current = (SemVersion)"9.1.0" }),
			["versioning-systems-current-required"] =
				c => WithStackSystem(c, new VersioningSystem { Id = VersioningSystemId.Stack, Base = (SemVersion)"8.0.0", Current = null! }),
			["base-not-after-current"] = c => WithStack(c, "9.2.0", "9.1.0"),
			["versioning-systems-complete"] =
				c => new VersioningSystemCatalog
				{
					VersioningSystems = c.VersioningSystems.Where(v => v.Key != VersioningSystemId.Ecs).ToDictionary()
				},
			["versionless-base-matches-current"] = c => WithStack(c, "9.0.0", "99999.0.0"),
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
	public void Laws() =>
		CodexPublishingConstraints.All.Laws(
			DomainGen.Of<CodexConfiguration>(),
			new Dictionary<string, Func<CodexConfiguration, CodexConfiguration>>
			{
				["environment-required"] = c => c with { Environment = " " }
			}
		);
}

public class CodexConfigurationConstraintsTests
{
	[Test]
	public void Laws() =>
		CodexConfigurationConstraints.All.Laws(
			DomainGen.Of<CodexConfiguration>(
				o => o.Identity<CodexGroupDefinition, string>(g => g.Id).Normalize<CodexConfiguration>(
					c =>
						c with
						{
							CanonicalBaseUrl = c.CanonicalBaseUrl is { } u && Uri.TryCreate(u, UriKind.Absolute, out _) ? u : null,
							Groups = [.. c.Groups.GroupBy(g => g.Id, StringComparer.OrdinalIgnoreCase).Select(g => g.First())]
						}
				)
			),
			CounterExamples
		);

	private static CodexGroupDefinition Group(string id, string name) => new() { Id = id, Name = name };

	private static IReadOnlyDictionary<string, Func<CodexConfiguration, CodexConfiguration>> CounterExamples =>
		new Dictionary<string, Func<CodexConfiguration, CodexConfiguration>>
		{
			["group-has-id"] = c => c with { Groups = [.. c.Groups, Group(" ", "Unnamed")] },
			["group-has-name"] = c => c with { Groups = [.. c.Groups, Group("search-unnamed", "")] },
			["group-ids-unique"] = c => c with { Groups = [.. c.Groups, Group("dup-group", "Dup"), Group("DUP-group", "Dup again")] },
			["codex-canonical-base-url-absolute"] = c => c with { CanonicalBaseUrl = "docs.example.test" }
		};
}
