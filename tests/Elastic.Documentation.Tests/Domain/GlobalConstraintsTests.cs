// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Constraints;
using Elastic.Documentation.Publishing;

namespace Elastic.Documentation.Tests.Domain;

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
					GoogleTagManager = new() { Enabled = true, Id = "GTM-1" }
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
			["google-tag-manager-has-id"] =
				a =>
					a with
					{
						Environments = new Dictionary<string, PublishEnvironment>
						{
							["prod"] = new() { Name = "prod", GoogleTagManager = new() { Enabled = true } }
						}
					}
		};
}

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
			["path-prefixes-unique"] =
				_ => With(Mount("kibana://reference", "reference/shared"), Mount("kibana://other", "reference/shared"))
		};
}
