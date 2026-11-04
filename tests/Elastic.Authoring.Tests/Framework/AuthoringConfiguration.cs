// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Versions;

namespace Elastic.Authoring.Tests.Framework;

/// <summary>
/// Versioning systems and products used by the authoring test harness.
///
/// These are intentionally different from <c>Elastic.Markdown.Tests.TestHelpers</c>:
/// the authoring harness registers 22 versioning systems and the three products
/// {elasticsearch→ElasticsearchProject, apm_agent_dotnet→ApmAgentDotnet, ecctl→Ecctl}
/// to match the original F# setup. Merging the two would silently change the behaviour
/// of the 90+ Applicability tests.
///
/// When adding new versioning systems, add them here AND in <c>TestHelpers.cs</c>
/// (or file an issue to extract both into a shared library).
/// </summary>
public static class AuthoringConfiguration
{
	// All standard versioning systems use SemVersion(8, 0, 0) except Serverless = AllVersions.
	private static readonly VersioningSystemId[] StandardSystems =
	[
		VersioningSystemId.Stack,
		VersioningSystemId.Self,
		VersioningSystemId.Ece,
		VersioningSystemId.Eck,
		VersioningSystemId.Ech,
		VersioningSystemId.ApmAgentDotnet,
		VersioningSystemId.ApmAgentNode,
		VersioningSystemId.Ecctl,
		VersioningSystemId.ElasticsearchProject,
		VersioningSystemId.Ess,
		VersioningSystemId.All,
		VersioningSystemId.ObservabilityProject,
		VersioningSystemId.SecurityProject,
		VersioningSystemId.ApmAgentJava,
		VersioningSystemId.ApmAgentPython,
		VersioningSystemId.EdotDotnet,
		VersioningSystemId.EdotJava,
		VersioningSystemId.EdotPython,
		VersioningSystemId.Curator,
		VersioningSystemId.EdotCollector
	];

	public static VersionsConfiguration CreateVersionsConfiguration()
	{
		var systems = StandardSystems.ToDictionary(
			id => id,
			id => new VersioningSystem { Id = id, Current = new SemVersion(8, 0, 0), Base = new SemVersion(8, 0, 0) }
		);
		// Serverless is the exception: uses AllVersions, not a SemVersion.
		systems[VersioningSystemId.Serverless] = new VersioningSystem
		{
			Id = VersioningSystemId.Serverless,
			Current = AllVersions.Instance,
			Base = AllVersions.Instance
		};
		return new VersionsConfiguration { VersioningSystems = systems };
	}

	public static ProductsConfiguration CreateProductsConfiguration(VersionsConfiguration versionsConfig)
	{
		var products = new Dictionary<string, Product>
		{
			{
				"elasticsearch",
				new Product
				{
					Id = "elasticsearch",
					DisplayName = "Elasticsearch",
					VersioningSystem = versionsConfig.GetVersioningSystem(VersioningSystemId.ElasticsearchProject)
				}
			},
			{
				"apm_agent_dotnet",
				new Product
				{
					Id = "apm_agent_dotnet",
					DisplayName = "APM Agent for .NET",
					VersioningSystem = versionsConfig.GetVersioningSystem(VersioningSystemId.ApmAgentDotnet)
				}
			},
			{
				"ecctl",
				new Product
				{
					Id = "ecctl",
					DisplayName = "Elastic Cloud Control ECCTL",
					VersioningSystem = versionsConfig.GetVersioningSystem(VersioningSystemId.Ecctl)
				}
			}
		};
		return new ProductsConfiguration
		{
			Products = products.ToFrozenDictionary(),
			PublicReferenceProducts = products.ToFrozenDictionary(),
			ProductDisplayNames = products.ToDictionary(p => p.Key, p => p.Value.DisplayName).ToFrozenDictionary()
		};
	}
}
