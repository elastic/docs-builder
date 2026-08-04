// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Changelog.Backfill;
using Elastic.Changelog.Backfill.Inventory;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Versions;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Backfill;

[SuppressMessage("Usage", "CA1001:Types that own disposable fields should be disposable")]
public class InventoryCensusServiceTests(ITestOutputHelper output)
{
	private const string SeedPath = "/repo/backfill-sources.yml";
	private const string OutputPath = "/repo/.artifacts/backfill-inventory.json";

	private readonly MockFileSystem _fileSystem = new();
	private readonly TestDiagnosticsCollector _collector = new(output);
	private readonly IConfigurationContext _configurationContext = A.Fake<IConfigurationContext>();

	private static VersioningSystem Versioning(VersioningSystemId id) => new()
	{
		Id = id,
		Current = new SemVersion(9, 2, 0),
		Base = new SemVersion(9, 2, 0)
	};

	private static Product MakeProduct(string id, VersioningSystemId? versioning = VersioningSystemId.Stack, bool releaseNotes = true) => new()
	{
		Id = id,
		DisplayName = id,
		VersioningSystem = versioning is { } v ? Versioning(v) : null,
		Features = new ProductFeatures { PublicReference = true, ReleaseNotes = releaseNotes }
	};

	private void SetupProducts(params Product[] products)
	{
		var dictionary = products.ToDictionary(p => p.Id, p => p);
		var configuration = new ProductsConfiguration
		{
			Products = dictionary.ToFrozenDictionary(),
			PublicReferenceProducts = dictionary.ToFrozenDictionary(),
			ProductDisplayNames = dictionary.ToDictionary(p => p.Key, p => p.Value.DisplayName).ToFrozenDictionary()
		};
		A.CallTo(() => _configurationContext.ProductsConfiguration).Returns(configuration);
	}

	private InventoryCensusService CreateService() =>
		new(NullLoggerFactory.Instance, _configurationContext, _fileSystem);

	private async Task<bool> RunAsync(string? seedYaml = null, IReadOnlyList<string>? allowRepos = null)
	{
		if (seedYaml is not null)
			_fileSystem.AddFile(SeedPath, new MockFileData(seedYaml));

		return await CreateService().BuildInventoryAsync(_collector, new BuildInventoryArguments
		{
			SourcesPath = seedYaml is not null ? SeedPath : null,
			OutputPath = OutputPath,
			AllowRepos = allowRepos ?? []
		}, TestContext.Current.CancellationToken);
	}

	private InventoryDocument ReadOutput() =>
		BackfillDocuments.Deserialize<InventoryDocument>(_fileSystem.File.ReadAllText(OutputPath));

	[Fact]
	public async Task BuildInventoryAsync_NoSeed_EveryProductIsVisibleAsUnresolved()
	{
		SetupProducts(MakeProduct("elasticsearch"), MakeProduct("kibana"));

		var result = await RunAsync();

		result.Should().BeTrue();
		var inventory = ReadOutput();
		inventory.Sources.Should().HaveCount(2);
		inventory.Sources.Should().OnlyContain(s => s.Classification == SourceClassification.SourceUnresolved);
		_collector.Diagnostics.Should().HaveCount(2).And.OnlyContain(d => d.Message.Contains("source-unresolved"));
	}

	[Fact]
	public async Task BuildInventoryAsync_ProductsWithoutReleaseNotesFeature_AreExcluded()
	{
		SetupProducts(MakeProduct("elasticsearch"), MakeProduct("docs-internal", releaseNotes: false));

		var result = await RunAsync();

		result.Should().BeTrue();
		ReadOutput().Sources.Should().ContainSingle(s => s.ProductIds.Contains("elasticsearch"));
	}

	[Fact]
	public async Task BuildInventoryAsync_MappedStackProduct_GetsStackDefaultCutoff()
	{
		SetupProducts(MakeProduct("elasticsearch"));

		var result = await RunAsync("""
			sources:
			  - repository: elastic/docs-content
			    git_ref: main
			    paths: [release-notes/elasticsearch]
			    products: [elasticsearch]
			    target_scheme: semver
			    adoption: not-adopted
			    classification: published-history-found
			""");

		result.Should().BeTrue();
		var source = ReadOutput().Sources.Single();
		source.Classification.Should().Be(SourceClassification.PublishedHistoryFound);
		source.SourceRepository.Should().Be(new GitRepository { Owner = "elastic", Name = "docs-content" });
		source.Cutoff.Should().NotBeNull();
		source.Cutoff.Kind.Should().Be(CutoffKind.Version);
		source.Cutoff.Value.Should().Be("9.0.0");
	}

	[Fact]
	public async Task BuildInventoryAsync_ExplicitCutoff_IsNotOverridden()
	{
		SetupProducts(MakeProduct("elasticsearch"));

		var result = await RunAsync("""
			sources:
			  - repository: elastic/docs-content
			    git_ref: main
			    products: [elasticsearch]
			    target_scheme: semver
			    cutoff: { kind: version, value: 8.15.0, notes: earlier import }
			    adoption: not-adopted
			    classification: published-history-found
			""");

		result.Should().BeTrue();
		ReadOutput().Sources.Single().Cutoff!.Value.Should().Be("8.15.0");
	}

	[Fact]
	public async Task BuildInventoryAsync_NonStackSemverProduct_GetsNoDefaultCutoff()
	{
		SetupProducts(MakeProduct("edot-java", VersioningSystemId.All));

		var result = await RunAsync("""
			sources:
			  - repository: elastic/elastic-otel-java
			    git_ref: main
			    products: [edot-java]
			    target_scheme: semver
			    adoption: partially-adopted
			    classification: native-artifacts-found
			""");

		result.Should().BeTrue();
		ReadOutput().Sources.Single().Cutoff.Should().BeNull();
	}

	[Fact]
	public async Task BuildInventoryAsync_AttributedRepositories_GetAllowlistStatus()
	{
		SetupProducts(MakeProduct("cloud-hosted", VersioningSystemId.Ech));

		var result = await RunAsync("""
			sources:
			  - repository: elastic/docs-content
			    git_ref: main
			    products: [cloud-hosted]
			    target_scheme: monthly
			    attributed_repositories: [elastic/elasticsearch, elastic/cloud]
			    adoption: partially-adopted
			    classification: hybrid-page
			""",
			allowRepos: ["elastic/elasticsearch"]);

		result.Should().BeTrue();
		var attributed = ReadOutput().Sources.Single().AttributedRepositories;
		attributed.Should().HaveCount(2);
		attributed.Single(a => a.Repository.Name == "elasticsearch").OnScrubberAllowlist.Should().BeTrue();
		attributed.Single(a => a.Repository.Name == "cloud").OnScrubberAllowlist.Should().BeFalse();
	}

	[Fact]
	public async Task BuildInventoryAsync_UnmappedProduct_IsRecordedWithReason()
	{
		SetupProducts(MakeProduct("elasticsearch"), MakeProduct("kibana"));

		var result = await RunAsync("""
			sources:
			  - repository: elastic/docs-content
			    git_ref: main
			    products: [elasticsearch]
			    target_scheme: semver
			    adoption: not-adopted
			    classification: published-history-found
			unmapped:
			  - product: kibana
			    reason: Deferred to the stack family pass.
			""");

		result.Should().BeTrue();
		var kibana = ReadOutput().Sources.Single(s => s.ProductIds.Contains("kibana"));
		kibana.Classification.Should().Be(SourceClassification.SourceUnresolved);
		kibana.UnresolvedItems.Should().Contain("Deferred to the stack family pass.");
		_collector.Diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task BuildInventoryAsync_UnresolvedSchemes_DeriveFromVersioningSystem()
	{
		SetupProducts(
			MakeProduct("elasticsearch"),
			MakeProduct("cloud-serverless", VersioningSystemId.Serverless),
			MakeProduct("cloud-hosted", VersioningSystemId.Ech));

		var result = await RunAsync();

		result.Should().BeTrue();
		var sources = ReadOutput().Sources;
		sources.Single(s => s.ProductIds.Contains("elasticsearch")).TargetScheme.Should().Be(TargetScheme.Semver);
		sources.Single(s => s.ProductIds.Contains("cloud-serverless")).TargetScheme.Should().Be(TargetScheme.Date);
		sources.Single(s => s.ProductIds.Contains("cloud-hosted")).TargetScheme.Should().Be(TargetScheme.Monthly);
		sources.Should().OnlyContain(s => s.UnresolvedItems.Any(i => i.Contains("derived")));
	}

	[Fact]
	public async Task BuildInventoryAsync_UnknownProductInSeed_Fails()
	{
		SetupProducts(MakeProduct("elasticsearch"));

		var result = await RunAsync("""
			sources:
			  - repository: elastic/docs-content
			    git_ref: main
			    products: [not-a-product]
			    target_scheme: semver
			    adoption: not-adopted
			    classification: published-history-found
			""");

		result.Should().BeFalse();
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("not-a-product"));
		_fileSystem.File.Exists(OutputPath).Should().BeFalse();
	}

	[Fact]
	public async Task BuildInventoryAsync_ProductBothMappedAndUnmapped_Fails()
	{
		SetupProducts(MakeProduct("elasticsearch"));

		var result = await RunAsync("""
			sources:
			  - repository: elastic/docs-content
			    git_ref: main
			    products: [elasticsearch]
			    target_scheme: semver
			    adoption: not-adopted
			    classification: published-history-found
			unmapped:
			  - product: elasticsearch
			    reason: Also deferred?
			""");

		result.Should().BeFalse();
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("both mapped"));
	}

	[Fact]
	public async Task BuildInventoryAsync_UnmappedWithoutReason_Fails()
	{
		SetupProducts(MakeProduct("elasticsearch"));

		var result = await RunAsync("""
			unmapped:
			  - product: elasticsearch
			""");

		result.Should().BeFalse();
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("reason is required"));
	}

	[Fact]
	public async Task BuildInventoryAsync_SourceUnresolvedClassification_IsNotSeedable()
	{
		SetupProducts(MakeProduct("elasticsearch"));

		var result = await RunAsync("""
			sources:
			  - repository: elastic/docs-content
			    git_ref: main
			    products: [elasticsearch]
			    target_scheme: semver
			    adoption: not-adopted
			    classification: source-unresolved
			""");

		result.Should().BeFalse();
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("classification"));
	}

	[Fact]
	public async Task BuildInventoryAsync_MalformedRepository_Fails()
	{
		SetupProducts(MakeProduct("elasticsearch"));

		var result = await RunAsync("""
			sources:
			  - repository: just-a-name
			    git_ref: main
			    products: [elasticsearch]
			    target_scheme: semver
			    adoption: not-adopted
			    classification: published-history-found
			""");

		result.Should().BeFalse();
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("owner/name"));
	}

	[Fact]
	public async Task BuildInventoryAsync_MissingSeedFile_Fails()
	{
		SetupProducts(MakeProduct("elasticsearch"));

		var result = await CreateService().BuildInventoryAsync(_collector, new BuildInventoryArguments
		{
			SourcesPath = "/repo/nope.yml",
			OutputPath = OutputPath
		}, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("does not exist"));
	}

	[Fact]
	public async Task BuildInventoryAsync_SameInputs_ProduceTheSameHash()
	{
		SetupProducts(MakeProduct("elasticsearch"), MakeProduct("kibana"));

		(await RunAsync()).Should().BeTrue();
		var first = BackfillDocuments.ComputeHash(_fileSystem.File.ReadAllText(OutputPath));

		(await RunAsync()).Should().BeTrue();
		var second = BackfillDocuments.ComputeHash(_fileSystem.File.ReadAllText(OutputPath));

		second.Should().Be(first);
	}

	[Fact]
	public async Task BuildInventoryAsync_Output_RoundTripsThroughBackfillDocuments()
	{
		SetupProducts(MakeProduct("elasticsearch"));

		(await RunAsync()).Should().BeTrue();

		// ReadOutput() deserializes with full envelope/version/validation checks.
		var inventory = ReadOutput();
		inventory.Sources.Should().ContainSingle();
	}
}
