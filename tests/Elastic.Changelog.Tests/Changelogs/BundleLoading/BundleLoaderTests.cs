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
			    prs:
			    - "100"
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
			prs:
			  - "100"
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
		var filtered = service.FilterEntries(entries, null);

		// Assert
		filtered.Should().HaveCount(2);
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
		var filtered = service.FilterEntries(entries, publishBlocker);

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
		var filtered = service.FilterEntries(entries, publishBlocker);

		// Assert
		filtered.Should().HaveCount(1);
		filtered[0].Title.Should().Be("Public feature");
	}

	[Fact]
	public void FilterEntries_WithPublishBlocker_CombinesTypeAndAreaBlocking()
	{
		// Arrange
		var service = CreateService();
		var entries = new List<ChangelogEntry>
		{
			new() { Title = "Visible", Type = ChangelogEntryType.Feature, Areas = ["Search"] },
			new() { Title = "Hidden by type", Type = ChangelogEntryType.Regression, Areas = ["Search"] },
			new() { Title = "Hidden by area", Type = ChangelogEntryType.Feature, Areas = ["Internal"] }
		};

		var publishBlocker = new PublishBlocker
		{
			Types = ["regression"],
			Areas = ["Internal"]
		};

		// Act
		var filtered = service.FilterEntries(entries, publishBlocker);

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

	#region Amend File Merging Tests

	[Fact]
	public void LoadBundles_WithAmendFile_MergesEntriesIntoParentBundle()
	{
		// Arrange
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// language=yaml
		var parentBundle =
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - title: Original feature
			    type: feature
			""";
		// language=yaml
		var amendBundle =
			"""
			products: []
			entries:
			  - title: Late addition
			    type: enhancement
			""";
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.yaml", parentBundle);
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.amend-1.yaml", amendBundle);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(1);
		bundles[0].Version.Should().Be("9.3.0");
		bundles[0].Entries.Should().HaveCount(2);
		bundles[0].Entries.Select(e => e.Title).Should().Contain(["Original feature", "Late addition"]);
		_warnings.Should().BeEmpty();
	}

	[Fact]
	public void LoadBundles_WithMultipleAmendFiles_MergesAllIntoParent()
	{
		// Arrange
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// language=yaml
		var parentBundle =
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - title: Original feature
			    type: feature
			""";
		// language=yaml
		var amendBundle1 =
			"""
			products: []
			entries:
			  - title: First amendment
			    type: enhancement
			""";
		// language=yaml
		var amendBundle2 =
			"""
			products: []
			entries:
			  - title: Second amendment
			    type: bug-fix
			""";
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.yaml", parentBundle);
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.amend-1.yaml", amendBundle1);
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.amend-2.yaml", amendBundle2);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(1);
		bundles[0].Version.Should().Be("9.3.0");
		bundles[0].Entries.Should().HaveCount(3);
		bundles[0].Entries.Select(e => e.Title).Should().Contain(["Original feature", "First amendment", "Second amendment"]);
	}

	[Fact]
	public void LoadBundles_AmendFileWithoutParent_RemainsStandalone()
	{
		// Arrange
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// No parent bundle, only amend file
		// language=yaml
		var amendBundle =
			"""
			products: []
			entries:
			  - title: Orphan entry
			    type: feature
			""";
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.amend-1.yaml", amendBundle);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(1);
		bundles[0].Version.Should().Be("9.3.0.amend-1"); // Falls back to filename without parent
		bundles[0].Entries.Should().HaveCount(1);
	}

	[Fact]
	public void LoadBundles_WithYmlExtension_AmendFileMergesCorrectly()
	{
		// Arrange
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// language=yaml
		var parentBundle =
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - title: Original feature
			    type: feature
			""";
		// language=yaml
		var amendBundle =
			"""
			products: []
			entries:
			  - title: Amendment
			    type: enhancement
			""";
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.yml", parentBundle);
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.amend-1.yml", amendBundle);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(1);
		bundles[0].Version.Should().Be("9.3.0");
		bundles[0].Entries.Should().HaveCount(2);
	}

	[Fact]
	public void LoadBundles_MixedExtensions_AmendFileMergesWithMatchingParent()
	{
		// Arrange
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// language=yaml
		var parentBundle =
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - title: Original
			    type: feature
			""";
		// language=yaml
		var amendBundle =
			"""
			products: []
			entries:
			  - title: Amendment
			    type: enhancement
			""";
		// Parent uses .yaml, amend uses .yml - they share the same base name
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.yaml", parentBundle);
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.amend-1.yaml", amendBundle);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(1);
		bundles[0].Entries.Should().HaveCount(2);
	}

	[Fact]
	public void LoadBundles_AmendPreservesParentMetadata()
	{
		// Arrange
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// language=yaml
		var parentBundle =
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    lifecycle: ga
			entries:
			  - title: Original
			    type: feature
			""";
		// language=yaml
		var amendBundle =
			"""
			products: []
			entries:
			  - title: Amendment
			    type: enhancement
			""";
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.yaml", parentBundle);
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.amend-1.yaml", amendBundle);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(1);
		bundles[0].Version.Should().Be("9.3.0");
		bundles[0].Repo.Should().Be("elasticsearch");
		bundles[0].FilePath.Should().EndWith("9.3.0.yaml");
		bundles[0].Data.Products.Should().HaveCount(1);
	}

	[Fact]
	public void LoadBundles_MultipleBundlesWithAmends_MergesCorrectly()
	{
		// Arrange
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// language=yaml
		var parent930 =
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - title: Feature 9.3.0
			    type: feature
			""";
		// language=yaml
		var amend930 =
			"""
			products: []
			entries:
			  - title: Amendment 9.3.0
			    type: enhancement
			""";
		// language=yaml
		var parent920 =
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - title: Feature 9.2.0
			    type: feature
			""";
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.yaml", parent930);
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.amend-1.yaml", amend930);
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.2.0.yaml", parent920);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(2);
		var bundle930 = bundles.Single(b => b.Version == "9.3.0");
		var bundle920 = bundles.Single(b => b.Version == "9.2.0");

		bundle930.Entries.Should().HaveCount(2);
		bundle930.Entries.Select(e => e.Title).Should().Contain(["Feature 9.3.0", "Amendment 9.3.0"]);

		bundle920.Entries.Should().HaveCount(1);
		bundle920.Entries[0].Title.Should().Be("Feature 9.2.0");
	}

	[Fact]
	public void LoadBundles_DateBasedBundleWithAmend_MergesCorrectly()
	{
		// Arrange - serverless-style date-based bundles
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// language=yaml
		var parentBundle =
			"""
			products:
			  - product: cloud-serverless
			    target: 2025-01-28
			entries:
			  - title: Serverless feature
			    type: feature
			""";
		// language=yaml
		var amendBundle =
			"""
			products: []
			entries:
			  - title: Late serverless fix
			    type: bug-fix
			""";
		_fileSystem.File.WriteAllText($"{bundlesFolder}/2025-01-28.yaml", parentBundle);
		_fileSystem.File.WriteAllText($"{bundlesFolder}/2025-01-28.amend-1.yaml", amendBundle);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(1);
		bundles[0].Version.Should().Be("2025-01-28");
		bundles[0].Entries.Should().HaveCount(2);
	}

	#endregion

	#region Repository Field Tests

	[Fact]
	public void LoadBundles_WithExplicitRepoField_UsesRepoInsteadOfProductId()
	{
		// Arrange
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// Bundle with explicit repo field that differs from product ID
		// language=yaml
		var bundleContent =
			"""
			products:
			  - product: cloud-serverless
			    target: 2025-01-28
			    repo: cloud
			entries:
			  - title: Test feature
			    type: feature
			""";
		_fileSystem.File.WriteAllText($"{bundlesFolder}/2025-01-28.yaml", bundleContent);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(1);
		bundles[0].Version.Should().Be("2025-01-28");
		// Repo should be "cloud" from explicit field, not "cloud-serverless" from ProductId
		bundles[0].Repo.Should().Be("cloud");
		_warnings.Should().BeEmpty();
	}

	[Fact]
	public void LoadBundles_WithoutRepoField_FallsBackToProductId()
	{
		// Arrange
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// Bundle without repo field - should fall back to product ID
		// language=yaml
		var bundleContent =
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - title: Test feature
			    type: feature
			""";
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.yaml", bundleContent);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(1);
		bundles[0].Version.Should().Be("9.3.0");
		// Repo should fall back to ProductId "elasticsearch"
		bundles[0].Repo.Should().Be("elasticsearch");
		_warnings.Should().BeEmpty();
	}

	[Fact]
	public void LoadBundles_WithEmptyRepoField_FallsBackToProductId()
	{
		// Arrange
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// Bundle with empty repo field - should fall back to product ID
		// language=yaml
		var bundleContent =
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    repo: ""
			entries:
			  - title: Test feature
			    type: feature
			""";
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.yaml", bundleContent);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(1);
		// Repo should fall back to ProductId when repo is empty
		bundles[0].Repo.Should().Be("elasticsearch");
		_warnings.Should().BeEmpty();
	}

	[Fact]
	public void LoadBundles_RepoFieldSerializesAndDeserializesCorrectly()
	{
		// Arrange
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// Bundle with repo field
		// language=yaml
		var bundleContent =
			"""
			products:
			  - product: elasticsearch-serverless
			    target: 2025-02-01
			    lifecycle: ga
			    repo: elasticsearch
			entries:
			  - title: Test feature
			    type: feature
			    prs:
			    - "123"
			""";
		_fileSystem.File.WriteAllText($"{bundlesFolder}/2025-02-01.yaml", bundleContent);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(1);
		bundles[0].Repo.Should().Be("elasticsearch");
		bundles[0].Data.Products.Should().HaveCount(1);
		bundles[0].Data.Products[0].Repo.Should().Be("elasticsearch");
		bundles[0].Data.Products[0].ProductId.Should().Be("elasticsearch-serverless");
		_warnings.Should().BeEmpty();
	}

	#endregion

	#region HideFeatures Tests

	[Fact]
	public void LoadBundles_WithHideFeaturesField_LoadsHideFeatures()
	{
		// Arrange
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// Bundle with hide-features field
		// language=yaml
		var bundleContent =
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			hide-features:
			  - feature:hidden-api
			  - feature:another-hidden
			entries:
			  - title: Test feature
			    type: feature
			""";
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.yaml", bundleContent);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(1);
		bundles[0].HideFeatures.Should().HaveCount(2);
		bundles[0].HideFeatures.Should().Contain("feature:hidden-api");
		bundles[0].HideFeatures.Should().Contain("feature:another-hidden");
		_warnings.Should().BeEmpty();
	}

	[Fact]
	public void LoadBundles_WithoutHideFeaturesField_ReturnsEmptyHideFeatures()
	{
		// Arrange
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		// Bundle without hide-features field
		// language=yaml
		var bundleContent =
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - title: Test feature
			    type: feature
			""";
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.yaml", bundleContent);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(1);
		bundles[0].HideFeatures.Should().BeEmpty();
		_warnings.Should().BeEmpty();
	}

	[Fact]
	public void LoadBundles_HideFeaturesSerializesAndDeserializesCorrectly()
	{
		// Arrange - Test round-trip serialization of hide-features field
		var bundlesFolder = "/docs/changelog/bundles";
		_fileSystem.Directory.CreateDirectory(bundlesFolder);

		var originalBundle = new Bundle
		{
			Products =
			[
				new BundledProduct { ProductId = "elasticsearch", Target = "9.3.0" }
			],
			HideFeatures = ["feature:first", "feature:second", "feature:third"],
			Entries =
			[
				new BundledEntry
				{
					Title = "Test feature",
					Type = ChangelogEntryType.Feature,
					File = new BundledFile { Name = "test.yaml", Checksum = "abc123" }
				}
			]
		};

		var serializedYaml = ReleaseNotesSerialization.SerializeBundle(originalBundle);
		_fileSystem.File.WriteAllText($"{bundlesFolder}/9.3.0.yaml", serializedYaml);

		var service = CreateService();

		// Act
		var bundles = service.LoadBundles(bundlesFolder, EmitWarning);

		// Assert
		bundles.Should().HaveCount(1);
		bundles[0].HideFeatures.Should().HaveCount(3);
		bundles[0].HideFeatures.Should().ContainInOrder("feature:first", "feature:second", "feature:third");
		bundles[0].Data.HideFeatures.Should().BeEquivalentTo(originalBundle.HideFeatures);
		_warnings.Should().BeEmpty();
	}

	[Fact]
	public void LoadedBundle_HideFeatures_ExposedFromBundleData()
	{
		// Arrange - Verify that LoadedBundle.HideFeatures properly exposes Data.HideFeatures
		var bundleData = new Bundle
		{
			Products = [],
			HideFeatures = ["feature:a", "feature:b"],
			Entries = []
		};
		var entries = new List<ChangelogEntry>();
		var bundle = new LoadedBundle("9.3.0", "elasticsearch", bundleData, "/path/to/bundle.yaml", entries);

		// Act
		var hideFeatures = bundle.HideFeatures;

		// Assert
		hideFeatures.Should().HaveCount(2);
		hideFeatures.Should().Contain("feature:a");
		hideFeatures.Should().Contain("feature:b");
		hideFeatures.Should().BeSameAs(bundleData.HideFeatures);
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
