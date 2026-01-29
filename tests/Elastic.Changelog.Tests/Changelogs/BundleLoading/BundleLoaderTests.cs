// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs.BundleLoading;

public class BundleLoaderTests(ITestOutputHelper output)
{
	private readonly ITestOutputHelper _output = output;
	private readonly MockFileSystem _fileSystem = new();
	private readonly List<string> _warnings = [];

	private BundleLoader CreateService() => new(_fileSystem);

	private void EmitWarning(string message)
	{
		_warnings.Add(message);
		_output.WriteLine($"Warning: {message}");
	}

	#region LoadBundles Tests

	[Fact]
	public void LoadBundles_WithValidBundles_ReturnsLoadedBundles()
	{
		// Arrange
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// language=yaml
		var bundleContent =
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - title: Test feature
			    type: feature
			    pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.yaml", bundleContent);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(1);
		bundles[0].Version.Should().Be("9.3.0");
		bundles[0].Repo.Should().Be("elasticsearch");
		bundles[0].Entries.Should().HaveCount(1);
		bundles[0].Entries[0].Title.Should().Be("Test feature");
		bundles[0].Entries[0].Type.Should().Be(ChangelogEntryType.Feature);
		_warnings.Should().BeEmpty();
	}

	[Fact]
	public void LoadBundles_WithMultipleBundles_ReturnsAllBundles()
	{
		// Arrange
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// language=yaml
		var bundle1 =
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - title: Feature in 9.3.0
			    type: feature
			""";
		// language=yaml
		var bundle2 =
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - title: Feature in 9.2.0
			    type: enhancement
			""";
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.yaml", bundle1);
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.2.0.yml", bundle2);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(2);
		bundles.Select(b => b.Version).Should().Contain(["9.3.0", "9.2.0"]);
		_warnings.Should().BeEmpty();
	}

	[Fact]
	public void LoadBundles_WithInvalidYaml_EmitsWarningAndSkips()
	{
		// Arrange
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// Invalid YAML - unclosed quote
		var invalidYaml = "products: [unclosed";
		_fileSystem.File.WriteAllText($"{bundlesFolder}/invalid.yaml", invalidYaml);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().BeEmpty();
		_warnings.Should().ContainSingle();
		_warnings[0].Should().Contain("Failed to parse changelog bundle 'invalid.yaml'");
	}

	[Fact]
	public void LoadBundles_WithNoProducts_UsesFilenameAsVersion()
	{
		// Arrange
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// language=yaml
		var bundleContent =
			"""
			products: []
			entries:
			  - title: Test entry
			    type: feature
			""";
		_fileSystem.File.WriteAllText($"{bundlesFolder}/2025-01-28.yaml", bundleContent);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(1);
		bundles[0].Version.Should().Be("2025-01-28");
		bundles[0].Repo.Should().Be("elastic");
	}

	#endregion

	#region ResolveEntries Tests

	[Fact]
	public void ResolveEntries_WithInlineEntries_ReturnsEntries()
	{
		// Arrange
		var service = CreateService();
		var bundle = new Bundle
		{
			Products =
			[
				new BundledProduct { ProductId = "elasticsearch", Target = "9.3.0" }
			],
			Entries =
			[
				new BundledEntry { Title = "Test feature", Type = ChangelogEntryType.Feature },
				new BundledEntry { Title = "Test fix", Type = ChangelogEntryType.BugFix }
			]
		};

		// Act
		var entries = service.ResolveEntries(bundle, "/changelog", EmitWarning);

		// Assert
		entries.Should().HaveCount(2);
		entries[0].Title.Should().Be("Test feature");
		entries[1].Title.Should().Be("Test fix");
		_warnings.Should().BeEmpty();
	}

	[Fact]
	public void ResolveEntries_WithFileReferences_LoadsFromFiles()
	{
		// Arrange
		var changelogDir = "/docs/changelog";
		_fileSystem.Directory.CreateDirectory($"{changelogDir}/entries");

		// language=yaml
		var entryContent =
			"""
			title: Feature from file
			type: feature
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: A feature loaded from a file
			""";
		_fileSystem.File.WriteAllText($"{changelogDir}/entries/feature.yaml", entryContent);

		var service = CreateService();
		var bundle = new Bundle
		{
			Products =
			[
				new BundledProduct { ProductId = "elasticsearch", Target = "9.3.0" }
			],
			Entries =
			[
				new BundledEntry { File = new BundledFile { Name = "entries/feature.yaml", Checksum = "sha1" } }
			]
		};

		// Act
		var entries = service.ResolveEntries(bundle, changelogDir, EmitWarning);

		// Assert
		entries.Should().HaveCount(1);
		entries[0].Title.Should().Be("Feature from file");
		entries[0].Description.Should().Be("A feature loaded from a file");
		_warnings.Should().BeEmpty();
	}

	[Fact]
	public void ResolveEntries_WithMissingFileReference_EmitsWarning()
	{
		// Arrange
		var changelogDir = "/docs/changelog";
		_fileSystem.Directory.CreateDirectory(changelogDir);

		var service = CreateService();
		var bundle = new Bundle
		{
			Products =
			[
				new BundledProduct { ProductId = "elasticsearch", Target = "9.3.0" }
			],
			Entries =
			[
				new BundledEntry { File = new BundledFile { Name = "nonexistent.yaml", Checksum = "sha1" } }
			]
		};

		// Act
		var entries = service.ResolveEntries(bundle, changelogDir, EmitWarning);

		// Assert
		entries.Should().BeEmpty();
		_warnings.Should().ContainSingle();
		_warnings[0].Should().Contain("not found");
	}

	[Fact]
	public void ResolveEntries_WithVersionField_NormalizesToTarget()
	{
		// Arrange
		var changelogDir = "/docs/changelog";
		_fileSystem.Directory.CreateDirectory(changelogDir);

		// Using legacy 'version:' field instead of 'target:'
		// language=yaml
		var entryContent =
			"""
			title: Legacy entry
			type: feature
			products:
			  - product: elasticsearch
			    version: 9.3.0
			""";
		_fileSystem.File.WriteAllText($"{changelogDir}/legacy.yaml", entryContent);

		var service = CreateService();
		var bundle = new Bundle
		{
			Products = [new BundledProduct { ProductId = "elasticsearch", Target = "9.3.0" }],
			Entries = [new BundledEntry { File = new BundledFile { Name = "legacy.yaml", Checksum = "sha1" } }]
		};

		// Act
		var entries = service.ResolveEntries(bundle, changelogDir, EmitWarning);

		// Assert
		entries.Should().HaveCount(1);
		entries[0].Title.Should().Be("Legacy entry");
		_warnings.Should().BeEmpty();
	}

	#endregion

	#region FilterEntries Tests

	[Fact]
	public void FilterEntries_WithNoFilters_ReturnsAllEntries()
	{
		// Arrange
		var service = CreateService();
		var entries = new List<ChangelogEntry>
		{
			new() { Title = "Entry 1", Type = ChangelogEntryType.Feature },
			new() { Title = "Entry 2", Type = ChangelogEntryType.BugFix }
		};

		// Act
		var filtered = service.FilterEntries(entries, null, []);

		// Assert
		filtered.Should().HaveCount(2);
	}

	[Fact]
	public void FilterEntries_WithFeatureIdFilter_HidesMatchingEntries()
	{
		// Arrange
		var service = CreateService();
		var entries = new List<ChangelogEntry>
		{
			new() { Title = "Public feature", Type = ChangelogEntryType.Feature },
			new() { Title = "Hidden feature", Type = ChangelogEntryType.Feature, FeatureId = "experimental-api" },
			new() { Title = "Another hidden", Type = ChangelogEntryType.Feature, FeatureId = "internal-only" }
		};

		var featureIdsToHide = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "experimental-api" };

		// Act
		var filtered = service.FilterEntries(entries, null, featureIdsToHide);

		// Assert
		filtered.Should().HaveCount(2);
		filtered.Select(e => e.Title).Should().Contain(["Public feature", "Another hidden"]);
		filtered.Select(e => e.Title).Should().NotContain("Hidden feature");
	}

	[Fact]
	public void FilterEntries_WithPublishBlocker_HidesBlockedTypes()
	{
		// Arrange
		var service = CreateService();
		var entries = new List<ChangelogEntry>
		{
			new() { Title = "Feature", Type = ChangelogEntryType.Feature },
			new() { Title = "Regression", Type = ChangelogEntryType.Regression },
			new() { Title = "Bug fix", Type = ChangelogEntryType.BugFix }
		};

		var publishBlocker = new PublishBlocker
		{
			Types = ["regression"]
		};

		// Act
		var filtered = service.FilterEntries(entries, publishBlocker, []);

		// Assert
		filtered.Should().HaveCount(2);
		filtered.Select(e => e.Type).Should().NotContain(ChangelogEntryType.Regression);
	}

	[Fact]
	public void FilterEntries_WithPublishBlocker_HidesBlockedAreas()
	{
		// Arrange
		var service = CreateService();
		var entries = new List<ChangelogEntry>
		{
			new() { Title = "Public feature", Type = ChangelogEntryType.Feature, Areas = ["Search"] },
			new() { Title = "Internal feature", Type = ChangelogEntryType.Feature, Areas = ["Internal"] },
			new() { Title = "Mixed feature", Type = ChangelogEntryType.Feature, Areas = ["Search", "Internal"] }
		};

		var publishBlocker = new PublishBlocker
		{
			Areas = ["Internal"]
		};

		// Act
		var filtered = service.FilterEntries(entries, publishBlocker, []);

		// Assert
		filtered.Should().HaveCount(1);
		filtered[0].Title.Should().Be("Public feature");
	}

	[Fact]
	public void FilterEntries_CombinesPublishBlockerAndFeatureIdFilter()
	{
		// Arrange
		var service = CreateService();
		var entries = new List<ChangelogEntry>
		{
			new() { Title = "Visible", Type = ChangelogEntryType.Feature },
			new() { Title = "Hidden by type", Type = ChangelogEntryType.Regression },
			new() { Title = "Hidden by feature", Type = ChangelogEntryType.Feature, FeatureId = "hidden" }
		};

		var publishBlocker = new PublishBlocker { Types = ["regression"] };
		var featureIdsToHide = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "hidden" };

		// Act
		var filtered = service.FilterEntries(entries, publishBlocker, featureIdsToHide);

		// Assert
		filtered.Should().HaveCount(1);
		filtered[0].Title.Should().Be("Visible");
	}

	#endregion

	#region MergeBundlesByTarget Tests

	[Fact]
	public void MergeBundlesByTarget_WithSingleBundle_ReturnsSameBundle()
	{
		// Arrange
		var service = CreateService();
		var bundles = new List<LoadedBundle>
		{
			new("9.3.0", "elasticsearch", new Bundle(), "/path/to/bundle.yaml",
				[new ChangelogEntry { Title = "Entry 1", Type = ChangelogEntryType.Feature }])
		};

		// Act
		var merged = service.MergeBundlesByTarget(bundles);

		// Assert
		merged.Should().HaveCount(1);
		merged[0].Should().BeSameAs(bundles[0]);
	}

	[Fact]
	public void MergeBundlesByTarget_WithDifferentVersions_KeepsSeparate()
	{
		// Arrange
		var service = CreateService();
		var bundles = new List<LoadedBundle>
		{
			new("9.3.0", "elasticsearch", new Bundle(), "/path/to/9.3.0.yaml",
				[new ChangelogEntry { Title = "Entry 9.3.0", Type = ChangelogEntryType.Feature }]),
			new("9.2.0", "elasticsearch", new Bundle(), "/path/to/9.2.0.yaml",
				[new ChangelogEntry { Title = "Entry 9.2.0", Type = ChangelogEntryType.Feature }])
		};

		// Act
		var merged = service.MergeBundlesByTarget(bundles);

		// Assert
		merged.Should().HaveCount(2);
	}

	[Fact]
	public void MergeBundlesByTarget_WithSameVersion_MergesEntries()
	{
		// Arrange
		var service = CreateService();
		var bundles = new List<LoadedBundle>
		{
			new("9.3.0", "elasticsearch", new Bundle(), "/path/to/es.yaml",
				[new ChangelogEntry { Title = "ES Entry", Type = ChangelogEntryType.Feature }]),
			new("9.3.0", "kibana", new Bundle(), "/path/to/kibana.yaml",
				[new ChangelogEntry { Title = "Kibana Entry", Type = ChangelogEntryType.Feature }])
		};

		// Act
		var merged = service.MergeBundlesByTarget(bundles);

		// Assert
		merged.Should().HaveCount(1);
		merged[0].Version.Should().Be("9.3.0");
		merged[0].Repo.Should().Be("elasticsearch+kibana");
		merged[0].Entries.Should().HaveCount(2);
		merged[0].Entries.Select(e => e.Title).Should().Contain(["ES Entry", "Kibana Entry"]);
	}

	[Fact]
	public void MergeBundlesByTarget_PreservesSortOrder()
	{
		// Arrange
		var service = CreateService();
		var bundles = new List<LoadedBundle>
		{
			new("9.2.0", "elasticsearch", new Bundle(), "/path/to/9.2.0.yaml", []),
			new("9.3.0", "elasticsearch", new Bundle(), "/path/to/9.3.0.yaml", []),
			new("9.1.0", "elasticsearch", new Bundle(), "/path/to/9.1.0.yaml", [])
		};

		// Act
		var merged = service.MergeBundlesByTarget(bundles);

		// Assert
		merged.Should().HaveCount(3);
		merged[0].Version.Should().Be("9.3.0");
		merged[1].Version.Should().Be("9.2.0");
		merged[2].Version.Should().Be("9.1.0");
	}

	[Fact]
	public void MergeBundlesByTarget_WithDateVersions_SortsCorrectly()
	{
		// Arrange - Date-based versions for serverless releases
		var service = CreateService();
		var bundles = new List<LoadedBundle>
		{
			new("2025-01-15", "cloud-serverless", new Bundle(), "/path/to/jan15.yaml", []),
			new("2025-01-28", "cloud-serverless", new Bundle(), "/path/to/jan28.yaml", []),
			new("2025-01-01", "cloud-serverless", new Bundle(), "/path/to/jan01.yaml", [])
		};

		// Act
		var merged = service.MergeBundlesByTarget(bundles);

		// Assert
		merged.Should().HaveCount(3);
		merged[0].Version.Should().Be("2025-01-28");
		merged[1].Version.Should().Be("2025-01-15");
		merged[2].Version.Should().Be("2025-01-01");
	}

	#endregion

	#region EntriesByType Tests

	[Fact]
	public void LoadedBundle_EntriesByType_GroupsCorrectly()
	{
		// Arrange
		var entries = new List<ChangelogEntry>
		{
			new() { Title = "Feature 1", Type = ChangelogEntryType.Feature },
			new() { Title = "Feature 2", Type = ChangelogEntryType.Feature },
			new() { Title = "Bug fix", Type = ChangelogEntryType.BugFix },
			new() { Title = "Breaking change", Type = ChangelogEntryType.BreakingChange }
		};
		var bundle = new LoadedBundle("9.3.0", "elasticsearch", new Bundle(), "/path/to/bundle.yaml", entries);

		// Act
		var byType = bundle.EntriesByType;

		// Assert
		byType.Should().HaveCount(3);
		byType[ChangelogEntryType.Feature].Should().HaveCount(2);
		byType[ChangelogEntryType.BugFix].Should().HaveCount(1);
		byType[ChangelogEntryType.BreakingChange].Should().HaveCount(1);
	}

	#endregion
}
