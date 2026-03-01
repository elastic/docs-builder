// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MatchResult = Elastic.Documentation.Configuration.Assembler.AssemblyConfiguration.ContentSourceMatch;

namespace Elastic.Documentation.Configuration.Tests;


public class AssemblyConfigurationMatchTests
{
	private static ILoggerFactory LoggerFactory => NullLoggerFactory.Instance;

	private static MatchResult NoMatch => new(null, null, null, false);
	private static MatchResult Speculative => new(null, null, null, true);

	private static AssemblyConfiguration CreateConfiguration(Dictionary<string, Repository>? repositories = null)
	{
		repositories ??= new Dictionary<string, Repository>
		{
			["test-repo"] = new()
			{
				Name = "test-repo",
				GitReferenceCurrent = "8.0",
				GitReferenceNext = "8.1",
				GitReferenceEdge = "main"
			}
		};

		var config = new AssemblyConfiguration
		{
			ReferenceRepositories = repositories,
			Narrative = new NarrativeRepository()
		};

		// Simulate the deserialization process that sets AvailableRepositories
		config.GetType().GetProperty("AvailableRepositories")!
			.SetValue(config, repositories.Values.Concat([config.Narrative]).ToDictionary(r => r.Name, r => r));

		return config;
	}

	private static Repository CreateRepository(string current = "8.0", string next = "8.1", string edge = "main") =>
		new()
		{
			Name = "test-repo",
			GitReferenceCurrent = current,
			GitReferenceNext = next,
			GitReferenceEdge = edge
		};

	private static Product CreateProduct(SemVersion currentVersion) =>
		new()
		{
			Id = "test-product",
			DisplayName = "Test Product",
			VersioningSystem = new VersioningSystem
			{
				Id = VersioningSystemId.Stack,
				Current = currentVersion,
				Base = new SemVersion(8, 0, 0)
			}
		};

	[Theory]
	[InlineData("test-repo")]
	[InlineData("other/test-repo")]
	public void InvalidRepositoryFormatReturnsNoMatch(string repository)
	{
		var config = CreateConfiguration();

		var result = config.Match(LoggerFactory, repository, "main", null, false);

		result.Should().BeEquivalentTo(NoMatch);
	}

	[Theory]
	[InlineData("main")]
	[InlineData("master")]
	[InlineData("8.15")]
	public void UnknownElasticRepositoryReturnsSpeculativeForIntegrationBranches(string branch)
	{
		var config = CreateConfiguration();

		var result = config.Match(LoggerFactory, "elastic/unknown-repo", branch, null, false);

		result.Should().BeEquivalentTo(Speculative);
	}

	[Fact]
	public void UnknownElasticRepositoryReturnsNoMatchForFeatureBranches()
	{
		var config = CreateConfiguration();

		var result = config.Match(LoggerFactory, "elastic/unknown-repo", "feature-branch", null, false);

		result.Should().BeEquivalentTo(NoMatch);
	}

	[Theory]
	[InlineData("8.0", ContentSource.Current)]
	[InlineData("8.1", ContentSource.Next)]
	[InlineData("main", ContentSource.Edge)]
	public void MatchesCorrectContentSource(string branch, ContentSource expectedSource)
	{
		var config = CreateConfiguration();

		var result = config.Match(LoggerFactory, "elastic/test-repo", branch, null, false);

		// Version branches set Speculative if they're >= current version (8.0)
		var isVersionBranch = branch.Contains('.');
		var speculative = isVersionBranch; // Both 8.0 and 8.1 are >= 8.0

		var expected = expectedSource switch
		{
			ContentSource.Current => new MatchResult(ContentSource.Current, null, null, speculative),
			ContentSource.Next => new MatchResult(null, ContentSource.Next, null, speculative),
			ContentSource.Edge => new MatchResult(null, null, ContentSource.Edge, false),
			_ => throw new ArgumentOutOfRangeException(nameof(expectedSource))
		};
		result.Should().BeEquivalentTo(expected);
	}

	[Fact]
	public void MatchesMultipleContentSourcesWhenBranchMatchesAll()
	{
		var repositories = new Dictionary<string, Repository>
		{
			["test-repo"] = CreateRepository(current: "main", next: "main", edge: "main")
		};
		var config = CreateConfiguration(repositories);

		var result = config.Match(LoggerFactory, "elastic/test-repo", "main", null, false);

		result.Should().BeEquivalentTo(new MatchResult(
			ContentSource.Current,
			ContentSource.Next,
			ContentSource.Edge,
			false
		));
	}

	[Theory]
	[InlineData("8.15", "8.0", true)]  // Greater than current
	[InlineData("8.15", "8.15", true)] // Equal to current
	[InlineData("8.0", "8.15", false)] // Less than current
	public void VersionBranchSpeculativeBuildBasedOnCurrentVersion(string branch, string currentVersion, bool shouldBeSpeculative)
	{
		var repositories = new Dictionary<string, Repository>
		{
			["test-repo"] = CreateRepository(current: currentVersion, next: "main", edge: "main")
		};
		var config = CreateConfiguration(repositories);

		var result = config.Match(LoggerFactory, "elastic/test-repo", branch, null, false);

		result.Speculative.Should().Be(shouldBeSpeculative);
	}

	[Theory]
	[InlineData("8.16", "8.15", true)]  // Greater than product version
	[InlineData("8.15", "8.15", false)] // Equal to product version — current is served from main
	[InlineData("8.14", "8.15", false)] // Previous minor version - but current is not versioned, so no previous minor logic
	[InlineData("8.13", "8.15", false)] // Less than previous minor
	[InlineData("8.0", "8.0", false)]   // Edge case: equal at minor version 0
	public void VersionBranchSpeculativeBuildBasedOnProductVersion(string branch, string productVersion, bool shouldBeSpeculative)
	{
		var repositories = new Dictionary<string, Repository>
		{
			["test-repo"] = CreateRepository(current: "main", next: "main", edge: "main")
		};
		var config = CreateConfiguration(repositories);
		var versionParts = productVersion.Split('.');
		var product = CreateProduct(new SemVersion(int.Parse(versionParts[0], null), int.Parse(versionParts[1], null), 0));

		var result = config.Match(LoggerFactory, "elastic/test-repo", branch, product, false);

		result.Speculative.Should().Be(shouldBeSpeculative);
	}

	[Theory]
	[InlineData("main")]
	[InlineData("master")]
	public void FallbackToSpeculativeBuildForMainOrMasterWhenNoMatch(string branch)
	{
		var repositories = new Dictionary<string, Repository>
		{
			["test-repo"] = CreateRepository(current: "8.0", next: "8.1", edge: "8.2")
		};
		var config = CreateConfiguration(repositories);

		var result = config.Match(LoggerFactory, "elastic/test-repo", branch, null, false);

		result.Current.Should().BeNull();
		result.Next.Should().BeNull();
		result.Edge.Should().BeNull();
		result.Speculative.Should().BeTrue();
	}

	[Fact]
	public void NoFallbackToSpeculativeBuildForFeatureBranches()
	{
		var repositories = new Dictionary<string, Repository>
		{
			["test-repo"] = CreateRepository(current: "8.0", next: "8.1", edge: "8.2")
		};
		var config = CreateConfiguration(repositories);

		var result = config.Match(LoggerFactory, "elastic/test-repo", "feature-branch", null, false);

		result.Should().BeEquivalentTo(NoMatch);
	}

	[Fact]
	public void DoesNotFallbackToSpeculativeWhenContentSourceMatched()
	{
		var repositories = new Dictionary<string, Repository>
		{
			["test-repo"] = CreateRepository(current: "main", next: "8.1", edge: "8.2")
		};
		var config = CreateConfiguration(repositories);

		var result = config.Match(LoggerFactory, "elastic/test-repo", "main", null, false);

		result.Current.Should().Be(ContentSource.Current);
	}

	[Fact]
	public void HandlesInvalidVersionBranchGracefully()
	{
		var config = CreateConfiguration();

		var result = config.Match(LoggerFactory, "elastic/test-repo", "8.x", null, false);

		result.Should().NotBeNull();
	}

	[Fact]
	public void ExtractsRepositoryNameFromFullPath()
	{
		var config = CreateConfiguration();

		var result = config.Match(LoggerFactory, "elastic/test-repo", "8.0", null, false);

		result.Current.Should().Be(ContentSource.Current);
	}

	[Fact]
	public void CurrentVersionMatchAlsoSetsSpeculative()
	{
		var repositories = new Dictionary<string, Repository>
		{
			["test-repo"] = CreateRepository(current: "8.15", next: "main", edge: "main")
		};
		var config = CreateConfiguration(repositories);

		var result = config.Match(LoggerFactory, "elastic/test-repo", "8.15", null, false);

		result.Current.Should().Be(ContentSource.Current);
		result.Speculative.Should().BeTrue();
	}

	[Theory]
	[InlineData("9.1", "9.0.0")]   // Greater than anchored product version
	[InlineData("9.5", "9.0.0")]   // Much greater than anchored product version
	public void VersionBranchSpeculativeBuildWhenGreaterThanAnchoredProductVersion(string branch, string productVersion)
	{
		var repositories = new Dictionary<string, Repository>
		{
			["test-repo"] = CreateRepository(current: "main", next: "main", edge: "main")
		};
		var config = CreateConfiguration(repositories);
		var versionParts = productVersion.Split('.');
		var product = CreateProduct(new SemVersion(int.Parse(versionParts[0], null), int.Parse(versionParts[1], null), int.Parse(versionParts[2], null)));

		var result = config.Match(LoggerFactory, "elastic/test-repo", branch, product, false);

		result.Speculative.Should().BeTrue();
	}

	[Theory]
	[InlineData("8.15", "9.0.0")]  // Less than anchored product version
	[InlineData("7.17", "9.0.0")]  // Much less than anchored product version
	[InlineData("8.0", "9.1.5")]   // Less than anchored product version with patch
	[InlineData("9.0", "9.0.0")]   // Equal to anchored product version — current is served from main
	public void VersionBranchNoSpeculativeBuildWhenLessThanOrEqualToAnchoredProductVersionAndNotPreviousMinor(string branch, string productVersion)
	{
		var repositories = new Dictionary<string, Repository>
		{
			["test-repo"] = CreateRepository(current: "main", next: "main", edge: "main")
		};
		var config = CreateConfiguration(repositories);
		var versionParts = productVersion.Split('.');
		var product = CreateProduct(new SemVersion(int.Parse(versionParts[0], null), int.Parse(versionParts[1], null), int.Parse(versionParts[2], null)));

		var result = config.Match(LoggerFactory, "elastic/test-repo", branch, product, false);

		result.Speculative.Should().BeFalse();
	}

	[Theory]
	[InlineData("9.1", "9.2")]   // Previous minor version - current is versioned branch
	[InlineData("8.14", "8.15")] // Previous minor version - current is versioned branch
	[InlineData("10.0", "10.1")] // Previous minor version at major boundary - current is versioned branch
	public void VersionBranchSpeculativeBuildWhenMatchesPreviousMinorVersion(string branch, string currentVersion)
	{
		var repositories = new Dictionary<string, Repository>
		{
			["test-repo"] = CreateRepository(current: currentVersion, next: "main", edge: "main")
		};
		var config = CreateConfiguration(repositories);

		var result = config.Match(LoggerFactory, "elastic/test-repo", branch, null, false);

		result.Speculative.Should().BeTrue();
	}

	[Fact]
	public void VersionBranchNoSpeculativeBuildWhenProductVersioningSystemIsNull()
	{
		var repositories = new Dictionary<string, Repository>
		{
			["test-repo"] = CreateRepository(current: "main", next: "main", edge: "main")
		};
		var config = CreateConfiguration(repositories);
		var product = new Product
		{
			Id = "test-product",
			DisplayName = "Test Product",
			VersioningSystem = null // No versioning system
		};

		var result = config.Match(LoggerFactory, "elastic/test-repo", "9.0", product, false);

		result.Speculative.Should().BeFalse();
	}

	[Fact]
	public void VersionBranchNoSpeculativeBuildWhenProductIsNull()
	{
		var repositories = new Dictionary<string, Repository>
		{
			["test-repo"] = CreateRepository(current: "main", next: "main", edge: "main")
		};
		var config = CreateConfiguration(repositories);

		var result = config.Match(LoggerFactory, "elastic/test-repo", "9.0", null, false);

		result.Speculative.Should().BeFalse();
	}

	[Theory]
	[InlineData("9.1", "9.0.15")]  // Anchored to 9.0.0, branch 9.1 > 9.0.0
	[InlineData("9.1", "9.0.0")]   // Anchored to 9.0.0, branch 9.1 > 9.0.0
	[InlineData("9.1", "9.0.1")]   // Anchored to 9.0.0, branch 9.1 > 9.0.0
	public void VersionBranchAnchorsProductVersionToMinorZero(string branch, string productVersion)
	{
		var repositories = new Dictionary<string, Repository>
		{
			["test-repo"] = CreateRepository(current: "main", next: "main", edge: "main")
		};
		var config = CreateConfiguration(repositories);
		var versionParts = productVersion.Split('.');
		var product = CreateProduct(new SemVersion(int.Parse(versionParts[0], null), int.Parse(versionParts[1], null), int.Parse(versionParts[2], null)));

		var result = config.Match(LoggerFactory, "elastic/test-repo", branch, product, false);

		result.Speculative.Should().BeTrue();
	}

	[Theory]
	[InlineData("8.0", "8.1")]  // Previous minor when current is 8.1
	[InlineData("7.17", "8.0")] // NOT previous minor when current is 8.0 (previous would be 7.0, not 7.17)
	public void VersionBranchPreviousMinorCalculationHandlesEdgeCases(string branch, string currentVersion)
	{
		var repositories = new Dictionary<string, Repository>
		{
			["test-repo"] = CreateRepository(current: currentVersion, next: "main", edge: "main")
		};
		var config = CreateConfiguration(repositories);

		var result = config.Match(LoggerFactory, "elastic/test-repo", branch, null, false);

		// 8.0 should match previous minor of 8.1 (which is 8.0)
		// 7.17 should NOT match previous minor of 8.0 (which is Math.Max(8-1, 0).0 = 7.0, not 7.17)
		var expectedSpeculative = branch == "8.0" && currentVersion == "8.1";
		result.Speculative.Should().Be(expectedSpeculative);
	}

	[Theory]
	[InlineData("9.1", "9.0.0")]   // Greater than anchored product version
	[InlineData("9.5", "9.0.0")]   // Much greater than anchored product version
	public void AlreadyPublishingTruePreventSpeculativeBuildForVersionBranch(string branch, string productVersion)
	{
		var repositories = new Dictionary<string, Repository>
		{
			["test-repo"] = CreateRepository(current: "main", next: "main", edge: "main")
		};
		var config = CreateConfiguration(repositories);
		var versionParts = productVersion.Split('.');
		var product = CreateProduct(new SemVersion(int.Parse(versionParts[0], null), int.Parse(versionParts[1], null), int.Parse(versionParts[2], null)));

		var result = config.Match(LoggerFactory, "elastic/test-repo", branch, product, true);

		result.Speculative.Should().BeFalse();
	}

	[Theory]
	[InlineData("9.1", "9.0.0")]   // Greater than anchored product version
	[InlineData("9.5", "9.0.0")]   // Much greater than anchored product version
	public void AlreadyPublishingFalseAllowsSpeculativeBuildForVersionBranch(string branch, string productVersion)
	{
		var repositories = new Dictionary<string, Repository>
		{
			["test-repo"] = CreateRepository(current: "main", next: "main", edge: "main")
		};
		var config = CreateConfiguration(repositories);
		var versionParts = productVersion.Split('.');
		var product = CreateProduct(new SemVersion(int.Parse(versionParts[0], null), int.Parse(versionParts[1], null), int.Parse(versionParts[2], null)));

		var result = config.Match(LoggerFactory, "elastic/test-repo", branch, product, false);

		result.Speculative.Should().BeTrue();
	}

	[Fact]
	public void AlreadyPublishingOnlyAffectsVersionBranchesWithoutVersionedCurrent()
	{
		var repositories = new Dictionary<string, Repository>
		{
			["test-repo"] = CreateRepository(current: "8.15", next: "main", edge: "main")
		};
		var config = CreateConfiguration(repositories);

		// When current is a version branch, alreadyPublishing should have no effect
		var resultTrue = config.Match(LoggerFactory, "elastic/test-repo", "8.15", null, true);
		var resultFalse = config.Match(LoggerFactory, "elastic/test-repo", "8.15", null, false);

		resultTrue.Speculative.Should().BeTrue();
		resultFalse.Speculative.Should().BeTrue();
	}

	[Theory]
	[InlineData("main")]
	[InlineData("master")]
	public void AlreadyPublishingDoesNotAffectNonVersionBranchesWithFallback(string branch)
	{
		var repositories = new Dictionary<string, Repository>
		{
			["test-repo"] = CreateRepository(current: "8.0", next: "8.1", edge: "8.2")
		};
		var config = CreateConfiguration(repositories);

		// alreadyPublishing should not affect main/master branches when they fall back to speculative
		var resultTrue = config.Match(LoggerFactory, "elastic/test-repo", branch, null, true);
		var resultFalse = config.Match(LoggerFactory, "elastic/test-repo", branch, null, false);

		resultTrue.Speculative.Should().BeTrue();
		resultFalse.Speculative.Should().BeTrue();
	}
}
