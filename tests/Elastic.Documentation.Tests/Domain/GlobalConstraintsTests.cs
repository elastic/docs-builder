// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Codex;
using Elastic.Documentation.Constraints;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.Versions;

namespace Elastic.Documentation.Tests.Domain;

[InheritsTests]
public class RelatedLearningConstraintsTests : ConstraintSetTests<RelatedLearningCatalog>
{
	protected override IReadOnlyList<Constraint<RelatedLearningCatalog>> Constraints => RelatedLearningConstraints.All;

	private static RelatedLearningCatalog With(RelatedLearningLink link) =>
		new() { Links = new Dictionary<string, RelatedLearningLink> { [link.Id] = link }.ToFrozenDictionary() };

	private static RelatedLearningLink Link => new() { Id = "intro", Title = "Introduction", Url = "https://www.elastic.co/intro" };

	protected override RelatedLearningCatalog Valid() => With(Link);

	protected override IReadOnlyDictionary<string, Func<RelatedLearningCatalog, RelatedLearningCatalog>> CounterExamples =>
		new Dictionary<string, Func<RelatedLearningCatalog, RelatedLearningCatalog>>
		{
			["link-has-title"] = _ => With(Link with { Title = "" }),
			["link-has-url"] = _ => With(Link with { Url = " " }),
			["link-url-absolute-http"] = _ => With(Link with { Url = "ftp://example.test/intro" })
		};
}

[InheritsTests]
public class AssemblyConstraintsTests : ConstraintSetTests<AssemblyConfiguration>
{
	protected override IReadOnlyList<Constraint<AssemblyConfiguration>> Constraints => AssemblyConstraints.All;

	protected override AssemblyConfiguration Valid()
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
					DeclaredContentSource = ContentSource.Current,
					GoogleTagManager = new() { Enabled = true, Id = "GTM-1" },
					Optimizely = new() { Enabled = true, Id = "1" }
				}
			},
			SharedConfigurations = new Dictionary<string, Repository>()
		};
	}

	protected override IReadOnlyDictionary<string, Func<AssemblyConfiguration, AssemblyConfiguration>> CounterExamples =>
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
			["content-source-declared"] = a => WithProd(a, e => e with { DeclaredContentSource = null }),
			["optimizely-has-id"] = a => WithProd(a, e => e with { Optimizely = new() { Enabled = true } })
		};

	private static AssemblyConfiguration WithProd(AssemblyConfiguration a, Func<PublishEnvironment, PublishEnvironment> change) =>
		a with { Environments = new Dictionary<string, PublishEnvironment> { ["prod"] = change(a.Environments["prod"]) } };
}

[InheritsTests]
public class SiteNavigationConstraintsTests : ConstraintSetTests<SiteNavigationDefinition>
{
	protected override IReadOnlyList<Constraint<SiteNavigationDefinition>> Constraints => SiteNavigationConstraints.All;

	private static SiteTableOfContentsRef Mount(string source, string pathPrefix) => new(new Uri(source), pathPrefix, []);

	private static SiteNavigationDefinition With(params SiteTableOfContentsRef[] mounts) =>
		new() { TableOfContents = [.. mounts.Select(m => new SiteNavigationEntry(m))] };

	protected override SiteNavigationDefinition Valid() =>
		With(Mount("kibana://reference", "reference/kibana"), Mount("elasticsearch://reference", "reference/elasticsearch"));

	protected override IReadOnlyDictionary<string, Func<SiteNavigationDefinition, SiteNavigationDefinition>> CounterExamples =>
		new Dictionary<string, Func<SiteNavigationDefinition, SiteNavigationDefinition>>
		{
			["mount-has-path-prefix"] = _ => With(Mount("kibana://reference", "")),
			["path-prefix-is-a-url-path"] = _ => With(Mount("kibana://reference", "a prefix with spaces")),
			["path-prefixes-unique"] =
				_ => With(Mount("kibana://reference", "reference/shared"), Mount("elasticsearch://reference", "reference/shared")),
			["repository-path-prefixes-unique"] =
				_ => With(Mount("kibana://reference", "reference/kibana"), Mount("kibana://other", "reference/kibana/"))
		};
}

[InheritsTests]
public class VersioningSystemConstraintsTests : ConstraintSetTests<VersioningSystemCatalog>
{
	protected override IReadOnlyList<Constraint<VersioningSystemCatalog>> Constraints => VersioningSystemConstraints.All;

	private static VersioningSystemCatalog With(string @base, string current) =>
		new()
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
			{
				[VersioningSystemId.Stack] = new()
				{
					Id = VersioningSystemId.Stack,
					Base = (SemVersion)@base,
					Current = (SemVersion)current
				}
			}
		};

	protected override VersioningSystemCatalog Valid() => With("8.0.0", "9.1.0");

	protected override IReadOnlyDictionary<string, Func<VersioningSystemCatalog, VersioningSystemCatalog>> CounterExamples =>
		new Dictionary<string, Func<VersioningSystemCatalog, VersioningSystemCatalog>>
		{
			["base-not-after-current"] = _ => With("9.2.0", "9.1.0")
		};
}

[InheritsTests]
public class CodexConfigurationConstraintsTests : ConstraintSetTests<CodexConfiguration>
{
	protected override IReadOnlyList<Constraint<CodexConfiguration>> Constraints => CodexConfigurationConstraints.All;

	private static CodexGroupDefinition Group(string id, string name) => new() { Id = id, Name = name };

	protected override CodexConfiguration Valid() =>
		new() { Groups = [Group("observability", "Observability"), Group("security", "Security")] };

	protected override IReadOnlyDictionary<string, Func<CodexConfiguration, CodexConfiguration>> CounterExamples =>
		new Dictionary<string, Func<CodexConfiguration, CodexConfiguration>>
		{
			["group-has-id"] = c => c with { Groups = [.. c.Groups, Group(" ", "Unnamed")] },
			["group-has-name"] = c => c with { Groups = [.. c.Groups, Group("search", "")] },
			["group-ids-unique"] = c => c with { Groups = [.. c.Groups, Group("Security", "Security again")] }
		};
}
