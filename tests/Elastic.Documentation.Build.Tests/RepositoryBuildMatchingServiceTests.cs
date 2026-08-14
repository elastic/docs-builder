// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Assembler.ContentSources;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Versions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Build.Tests;

public class RepositoryBuildMatchingServiceTests
{
	[Fact]
	public void CreateBuildMatchFacts_ConfiguredMainMatch_ExplainsBuild()
	{
		var config = CreateConfiguration(CreateRepository("kibana", current: "main", next: "main", edge: "main"));
		var product = CreateProduct("kibana", new SemVersion(9, 4, 0));
		var matches = config.Match(NullLoggerFactory.Instance, "elastic/kibana", "main", product, alreadyPublishing: false);

		var facts = RepositoryBuildMatchingService.CreateBuildMatchFacts(new RepositoryBuildMatchingService.BuildMatchInput
		{
			Configuration = config,
			Repository = "elastic/kibana",
			BranchOrTag = "main",
			RegistryKey = "kibana",
			Product = product,
			AlreadyPublishing = false,
			Matches = matches
		});

		facts.ShouldBuild.Should().BeTrue();
		facts.MatchedContentSources.Should().Be("current, next, edge");
		facts.Reason.Should().Be("ref matches configured content-source(s): current, next, edge");
		facts.ToMarkdown().Should().Contain("**Decision:** build")
			.And.Contain("| configured current | main |")
			.And.Contain("| publishing to link registry | False |");
	}

	[Fact]
	public void CreateBuildMatchFacts_AlreadyPublishingVersionBranchSkip_ExplainsRegistryGate()
	{
		var config = CreateConfiguration(CreateRepository("kibana", current: "main", next: "main", edge: "main"));
		var product = CreateProduct("kibana", new SemVersion(9, 0, 0));
		var matches = config.Match(NullLoggerFactory.Instance, "elastic/kibana", "9.1", product, alreadyPublishing: true);

		var facts = RepositoryBuildMatchingService.CreateBuildMatchFacts(new RepositoryBuildMatchingService.BuildMatchInput
		{
			Configuration = config,
			Repository = "elastic/kibana",
			BranchOrTag = "9.1",
			RegistryKey = "kibana",
			Product = product,
			AlreadyPublishing = true,
			Matches = matches
		});

		facts.ShouldBuild.Should().BeFalse();
		facts.MatchedContentSources.Should().Be("<none>");
		facts.Reason.Should().Be("configured current is not versioned and repository already publishes to the link registry, so version branches are not built speculatively");
		facts.ToMarkdown().Should().Contain("**Decision:** skip")
			.And.Contain("| ref | 9.1 |")
			.And.Contain("| publishing to link registry | True |");
	}

	private static AssemblyConfiguration CreateConfiguration(Repository repository)
	{
		var repositories = new Dictionary<string, Repository> { [repository.Name] = repository };
		var config = new AssemblyConfiguration
		{
			ReferenceRepositories = repositories,
			Narrative = new NarrativeRepository()
		};
		config.GetType().GetProperty("AvailableRepositories")!
			.SetValue(config, repositories.Values.Concat([config.Narrative]).ToDictionary(r => r.Name, r => r));
		return config;
	}

	private static Repository CreateRepository(string name, string current, string next, string edge) =>
		new()
		{
			Name = name,
			GitReferenceCurrent = current,
			GitReferenceNext = next,
			GitReferenceEdge = edge
		};

	private static Product CreateProduct(string id, SemVersion currentVersion) =>
		new()
		{
			Id = id,
			DisplayName = id,
			VersioningSystem = new VersioningSystem
			{
				Id = VersioningSystemId.Stack,
				Current = currentVersion,
				Base = new SemVersion(8, 0, 0)
			}
		};
}
