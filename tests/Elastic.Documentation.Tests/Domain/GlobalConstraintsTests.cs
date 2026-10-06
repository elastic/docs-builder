// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Codex;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.Versions;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class RelatedLearningConstraintsTests
{
	[Test]
	public void Laws() => RelatedLearningConstraints.All.Laws(Valid(), CounterExamples);

	private static RelatedLearningCatalog With(RelatedLearningLink link) =>
		new() { Links = new Dictionary<string, RelatedLearningLink> { [link.Id] = link }.ToFrozenDictionary() };

	private static RelatedLearningLink Link => new() { Id = "intro", Title = "Introduction", Url = "https://www.elastic.co/intro" };

	private static RelatedLearningCatalog Valid() => With(Link);

	private static IReadOnlyDictionary<string, Func<RelatedLearningCatalog, RelatedLearningCatalog>> CounterExamples =>
		new Dictionary<string, Func<RelatedLearningCatalog, RelatedLearningCatalog>>
		{
			["link-has-title"] = _ => With(Link with { Title = "" }),
			["link-has-url"] = _ => With(Link with { Url = " " }),
			["link-url-absolute-http"] = _ => With(Link with { Url = "ftp://example.test/intro" })
		};
}

public class AssemblyConstraintsTests
{
	[Test]
	public void Laws() => AssemblyConstraints.All.Laws(Valid(), CounterExamples);

	private static AssemblyConfiguration Valid()
	{
		var narrative = new NarrativeRepository();
		var kibana = new Repository { Name = "kibana" };
		return new AssemblyConfiguration
		{
			Narrative = narrative,
			ReferenceRepositories = new Dictionary<string, Repository> { ["kibana"] = kibana },
			AvailableRepositories = new Dictionary<string, Repository> { ["kibana"] = kibana, [narrative.Name] = narrative },
			PrivateRepositories = new Dictionary<string, Repository>(),
			AllPrivateRepositoryNames = new HashSet<string>(),
			Environments = new Dictionary<string, PublishEnvironment>
			{
				["prod"] = new()
				{
					Name = "prod",
					Uri = "https://www.elastic.co",
					ContentSource = ContentSource.Current,
					GoogleTagManager = new() { Enabled = true, Id = "GTM-1" },
					Optimizely = new() { Enabled = true, Id = "1" }
				}
			},
			SharedConfigurations = new Dictionary<string, Repository>()
		};
	}

	private static IReadOnlyDictionary<string, Func<AssemblyConfiguration, AssemblyConfiguration>> CounterExamples =>
		new Dictionary<string, Func<AssemblyConfiguration, AssemblyConfiguration>>
		{
			["narrative-name-reserved"] =
				a =>
					a with
					{
						ReferenceRepositories = new Dictionary<string, Repository>(a.ReferenceRepositories)
						{
							[NarrativeRepository.RepositoryName] = new() { Name = NarrativeRepository.RepositoryName }
						}
					},
			["google-tag-manager-has-id"] = a => WithProd(a, e => e with { GoogleTagManager = new() { Enabled = true } }),
			["environment-uri-absolute"] = a => WithProd(a, e => e with { Uri = "www.elastic.co" }),
			["optimizely-has-id"] = a => WithProd(a, e => e with { Optimizely = new() { Enabled = true } }),
			["environment-path-prefix-resolved"] = a => WithProd(a, e => e with { PathPrefix = "${ASSEMBLER_PREVIEW_PATH_PREFIX}" }),
			["repository-name-matches-key"] =
				a => a with { ReferenceRepositories = new Dictionary<string, Repository> { ["kibana"] = new() { Name = "beats" } } },
			["available-keys-match-names"] =
				a =>
					a with
					{
						AvailableRepositories = new Dictionary<string, Repository>
						{
							["kibana"] = new() { Name = "beats" },
							[a.Narrative.Name] = a.Narrative
						}
					},
			["private-keys-match-names"] =
				a =>
					a with
					{
						PrivateRepositories = new Dictionary<string, Repository> { ["kibana"] = new() { Name = "beats", Private = true } },
						AllPrivateRepositoryNames = new HashSet<string> { "kibana" }
					},
			["narrative-not-skipped"] = a => a with { Narrative = new NarrativeRepository { Skip = true } },
			["repository-sparse-paths-valid"] = a => OnlyReference(a, k => k with { SparsePaths = [" "] }),
			["repository-clone-timeout-positive"] = a => OnlyReference(a, k => k with { CloneTimeout = TimeSpan.Zero }),
			["repository-skipped"] = a => OnlyReference(a, k => k with { Skip = true }),
			["available-repositories-publish"] =
				a =>
					a with
					{
						AvailableRepositories = new Dictionary<string, Repository>(a.AvailableRepositories)
						{
							["kibana"] = a.ReferenceRepositories["kibana"] with { Skip = true }
						}
					},
			["repository-names-unique-ignoring-case"] =
				a =>
					a with
					{
						AvailableRepositories = new Dictionary<string, Repository>(a.AvailableRepositories)
						{
							["Kibana"] = new() { Name = "Kibana" }
						}
					},
			["private-is-available"] =
				a =>
					a with
					{
						PrivateRepositories = new Dictionary<string, Repository> { ["beats"] = new() { Name = "beats", Private = true } },
						AllPrivateRepositoryNames = new HashSet<string> { "beats" }
					},
			["private-is-listed"] =
				a => a with { PrivateRepositories = new Dictionary<string, Repository> { ["kibana"] = a.ReferenceRepositories["kibana"] } },
			["environment-name-in-runtime-list"] =
				a =>
					a with
					{
						Environments = new Dictionary<string, PublishEnvironment>
						{
							["preview"] = a.Environments["prod"] with { Name = "preview" }
						}
					},
			["feature-flag-keys-normalized"] =
				a => WithProd(a, e => e with { FeatureFlags = new Dictionary<string, bool> { ["PAGE_FEEDBACK"] = true } }),
			["environment-website-search-url-absolute"] = a => WithProd(a, e => e with { WebsiteSearchScriptUrl = "search/script.js" }),
			["repository-name-single-segment"] = a => a with { Narrative = new NarrativeRepository { Name = "docs/content" } },
			// The key stays in the runtime list; only the environment's own name is unusable in a store name.
			["environment-name-store-safe"] = a => WithProd(a, e => e with { Name = "pro d" }),
			["environment-name-lowercase"] = a => WithProd(a, e => e with { Name = "Prod" })
		};

	private static AssemblyConfiguration OnlyReference(AssemblyConfiguration a, Func<Repository, Repository> change) =>
		a with { ReferenceRepositories = new Dictionary<string, Repository> { ["kibana"] = change(a.ReferenceRepositories["kibana"]) } };

	private static AssemblyConfiguration WithProd(AssemblyConfiguration a, Func<PublishEnvironment, PublishEnvironment> change) =>
		a with { Environments = new Dictionary<string, PublishEnvironment> { ["prod"] = change(a.Environments["prod"]) } };
}

public class SiteNavigationConstraintsTests
{
	[Test]
	public void Laws() => SiteNavigationConstraints.All.Laws(Valid(), CounterExamples);

	private static SiteTableOfContentsRef Mount(string source, string pathPrefix) => new(new Uri(source), pathPrefix, []);

	private static SiteNavigationDefinition With(params SiteTableOfContentsRef[] mounts) =>
		new() { TableOfContents = [.. mounts.Select(m => new SiteNavigationEntry(m))] };

	private static SiteNavigationDefinition Plus(SiteNavigationDefinition navigation, params SiteTableOfContentsRef[] mounts) =>
		navigation with { TableOfContents = [.. navigation.TableOfContents, .. mounts.Select(m => new SiteNavigationEntry(m))] };

	private static SiteNavigationDefinition Plus(SiteNavigationDefinition navigation, params SiteSectionRef[] sections) =>
		navigation with { TableOfContents = [.. navigation.TableOfContents, .. sections.Select(s => new SiteNavigationEntry(s))] };

	private static SiteNavigationDefinition Valid() =>
		With(Mount("kibana://reference", "reference/kibana"), Mount("elasticsearch://reference", "reference/elasticsearch"));

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
			["mount-source-unique"] = n => Plus(n, Mount("kibana://reference", "reference/kibana-again")),
			["section-titles-unique"] =
				n => Plus(n, new SiteSectionRef("Alpha", "https://a.test", [], []), new SiteSectionRef("alpha", "https://b.test", [], [])),
			["dropdown-link-url-valid"] =
				n => Plus(n, new SiteSectionRef("Links", null, [], [new SiteDropdownLinkRef("Docs", "https://example.test/docs")])),
			["phantom-not-mounted"] = n => n with { Phantoms = [new PhantomRegistration { Source = "kibana://reference" }] },
			["mount-sources-segment-prefix-free"] = n => Plus(n, Mount("kibana://reference-foo", "reference/kibana-foo")),
			["mount-is-published"] = n => Plus(n, Mount("eland://sphinx", "reference/eland")),
			["entry-unknown-keys"] =
				n => Plus(n, Mount("logstash://reference", "reference/logstash") with { UnknownKeys = ["pathprefix"] }),
			["mount-repo-key"] = n => Plus(n, Mount("logstash://reference", "reference/logstash") with { UnknownKeys = ["repo"] }),
			["island-is-boolean"] = n => Plus(n, Mount("logstash://reference", "reference/logstash") with { IslandRaw = "yes" }),
			["mount-has-path-prefix"] = _ => With(Mount("kibana://reference", "")),
			["path-prefix-is-a-url-path"] = _ => With(Mount("kibana://reference", "a prefix with spaces")),
			["path-prefixes-unique"] =
				_ => With(Mount("kibana://reference", "reference/shared"), Mount("elasticsearch://reference", "reference/shared")),
			["repository-path-prefixes-unique"] =
				_ => With(Mount("kibana://reference", "reference/kibana"), Mount("kibana://other", "reference/kibana/"))
		};
}

public class VersioningSystemConstraintsTests
{
	[Test]
	public void Laws() => VersioningSystemConstraints.All.Laws(Valid(), CounterExamples);

	// Every id is defined, so only the rule under test is broken; the stack entry is the one the examples vary.
	private static VersioningSystemCatalog With(string @base, string current, VersioningSystemId? without = null) =>
		new()
		{
			VersioningSystems = VersioningSystemIdExtensions
				.GetValues()
				.Where(id => id != VersioningSystemId.None && id != without)
				.ToDictionary(
					id => id,
					id => new VersioningSystem
					{
						Id = id,
						Base = id == VersioningSystemId.Stack ? (SemVersion)@base : (SemVersion)"8.0.0",
						Current = id == VersioningSystemId.Stack ? (SemVersion)current : (SemVersion)"9.1.0"
					}
				)
		};

	private static VersioningSystemCatalog Valid() => With("8.0.0", "9.1.0");

	private static IReadOnlyDictionary<string, Func<VersioningSystemCatalog, VersioningSystemCatalog>> CounterExamples =>
		new Dictionary<string, Func<VersioningSystemCatalog, VersioningSystemCatalog>>
		{
			["base-not-after-current"] = _ => With("9.2.0", "9.1.0"),
			["versioning-systems-complete"] = _ => With("8.0.0", "9.1.0", without: VersioningSystemId.Ecs),
			["versionless-base-matches-current"] = _ => With("9.0.0", "99999.0.0")
		};
}

public class CodexPublishingConstraintsTests
{
	[Test]
	public void Laws() =>
		CodexPublishingConstraints.All.Laws(
			new CodexConfiguration { Environment = "internal" },
			new Dictionary<string, Func<CodexConfiguration, CodexConfiguration>>
			{
				["environment-required"] = c => c with { Environment = " " }
			}
		);
}

public class CodexConfigurationConstraintsTests
{
	[Test]
	public void Laws() => CodexConfigurationConstraints.All.Laws(Valid(), CounterExamples);

	private static CodexGroupDefinition Group(string id, string name) => new() { Id = id, Name = name };

	private static CodexConfiguration Valid() =>
		new() { Environment = "internal", Groups = [Group("observability", "Observability"), Group("security", "Security")] };

	private static IReadOnlyDictionary<string, Func<CodexConfiguration, CodexConfiguration>> CounterExamples =>
		new Dictionary<string, Func<CodexConfiguration, CodexConfiguration>>
		{
			["group-has-id"] = c => c with { Groups = [.. c.Groups, Group(" ", "Unnamed")] },
			["group-has-name"] = c => c with { Groups = [.. c.Groups, Group("search", "")] },
			["group-ids-unique"] = c => c with { Groups = [.. c.Groups, Group("Security", "Security again")] },
			["codex-canonical-base-url-absolute"] = c => c with { CanonicalBaseUrl = "docs.example.test" }
		};
}
