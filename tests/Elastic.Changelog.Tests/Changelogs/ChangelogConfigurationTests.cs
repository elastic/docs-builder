// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Configuration;
using Elastic.Changelog.Serialization;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Diagnostics;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs;

public class ChangelogConfigurationTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	[Fact]
	public async Task LoadChangelogConfiguration_WithoutPivot_UsesDefaults()
	{
		// Arrange
		var configLoader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config without pivot - should use defaults
		// language=yaml
		var configContent =
			"""
			lifecycles:
			  - ga
			""";
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var originalDir = FileSystem.Directory.GetCurrentDirectory();
		try
		{
			FileSystem.Directory.SetCurrentDirectory(configDir);

			// Act
			var config = await configLoader.LoadChangelogConfiguration(Collector, null, TestContext.Current.CancellationToken);

			// Assert
			config.Should().NotBeNull();
			Collector.Errors.Should().Be(0);
			// Should have default types
			config!.Types.Should().Contain("feature");
			config.Types.Should().Contain("bug-fix");
			config.Types.Should().Contain("docs");
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task LoadChangelogConfiguration_WithPivotTypes_UsesConfiguredTypes()
	{
		// Arrange
		var configLoader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config with pivot.types - should derive available types from keys
		// Must include required types: feature, bug-fix, breaking-change
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature: ">feature"
			    bug-fix: ">bug"
			    breaking-change: ">breaking"
			lifecycles:
			  - ga
			""";
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var originalDir = FileSystem.Directory.GetCurrentDirectory();
		try
		{
			FileSystem.Directory.SetCurrentDirectory(configDir);

			// Act
			var config = await configLoader.LoadChangelogConfiguration(Collector, null, TestContext.Current.CancellationToken);

			// Assert
			config.Should().NotBeNull();
			Collector.Errors.Should().Be(0);
			// Should have default subtypes (no pivot.subtypes defined)
			config!.SubTypes.Should().Contain("api");
			config.SubTypes.Should().Contain("behavioral");
			// Should have types from pivot.types keys
			config.Types.Should().Contain("feature");
			config.Types.Should().Contain("bug-fix");
			config.Types.Should().Contain("breaking-change");
			config.Types.Should().HaveCount(3);
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task LoadChangelogConfiguration_WithoutAvailableLifecycles_UsesDefaults()
	{
		// Arrange
		var configLoader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config without lifecycles - should use defaults
		// Must include required types: feature, bug-fix, breaking-change
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			""";
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var originalDir = FileSystem.Directory.GetCurrentDirectory();
		try
		{
			FileSystem.Directory.SetCurrentDirectory(configDir);

			// Act
			var config = await configLoader.LoadChangelogConfiguration(Collector, null, TestContext.Current.CancellationToken);

			// Assert
			config.Should().NotBeNull();
			Collector.Errors.Should().Be(0);
			// Should have default lifecycles (now strongly typed as Lifecycle enum)
			config!.Lifecycles.Should().Contain(Lifecycle.Preview);
			config.Lifecycles.Should().Contain(Lifecycle.Beta);
			config.Lifecycles.Should().Contain(Lifecycle.Ga);
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task LoadChangelogConfiguration_WithPivotAreas_ComputesLabelToAreasMapping()
	{
		// Arrange
		var configLoader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config with pivot.areas
		// Must include required types: feature, bug-fix, breaking-change
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			  areas:
			    Search: ":Search/Search"
			    Security: ":Security/Security"
			""";
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var originalDir = FileSystem.Directory.GetCurrentDirectory();
		try
		{
			FileSystem.Directory.SetCurrentDirectory(configDir);

			// Act
			var config = await configLoader.LoadChangelogConfiguration(Collector, null, TestContext.Current.CancellationToken);

			// Assert
			config.Should().NotBeNull();
			Collector.Errors.Should().Be(0);
			// Should have areas from pivot.areas keys
			config!.Areas.Should().NotBeNull();
			config.Areas.Should().Contain("Search");
			config.Areas.Should().Contain("Security");
			// Should have inverted label mappings
			config.LabelToAreas.Should().NotBeNull();
			config.LabelToAreas.Should().ContainKey(":Search/Search");
			config.LabelToAreas![":Search/Search"].Should().Be("Search");
			config.LabelToAreas.Should().ContainKey(":Security/Security");
			config.LabelToAreas[":Security/Security"].Should().Be("Security");
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task LoadChangelogConfiguration_WithPivotTypesLabels_ComputesLabelToTypeMapping()
	{
		// Arrange
		var configLoader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config with pivot.types having labels
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    breaking-change: ">breaking, >bc"
			    bug-fix: ">bug"
			    feature:
			""";
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var originalDir = FileSystem.Directory.GetCurrentDirectory();
		try
		{
			FileSystem.Directory.SetCurrentDirectory(configDir);

			// Act
			var config = await configLoader.LoadChangelogConfiguration(Collector, null, TestContext.Current.CancellationToken);

			// Assert
			config.Should().NotBeNull();
			Collector.Errors.Should().Be(0);
			// Should have inverted label mappings
			config!.LabelToType.Should().NotBeNull();
			config.LabelToType.Should().ContainKey(">breaking");
			config.LabelToType![">breaking"].Should().Be("breaking-change");
			config.LabelToType.Should().ContainKey(">bc");
			config.LabelToType[">bc"].Should().Be("breaking-change");
			config.LabelToType.Should().ContainKey(">bug");
			config.LabelToType[">bug"].Should().Be("bug-fix");
			// feature has no labels, so no mapping
			config.LabelToType.Should().NotContainKey("feature");
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task LoadChangelogConfiguration_WithInvalidPivotType_ReturnsError()
	{
		// Arrange
		var configLoader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config with invalid type in pivot.types
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			    invalid-type: ">invalid"
			""";
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var originalDir = FileSystem.Directory.GetCurrentDirectory();
		try
		{
			FileSystem.Directory.SetCurrentDirectory(configDir);

			// Act
			var config = await configLoader.LoadChangelogConfiguration(Collector, null, TestContext.Current.CancellationToken);

			// Assert
			config.Should().BeNull();
			Collector.Errors.Should().BeGreaterThan(0);
			Collector.Diagnostics.Should().Contain(d =>
				d.Severity == Severity.Error &&
				d.Message.Contains("Type 'invalid-type' in pivot.types") &&
				d.Message.Contains("is not a valid type"));
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task LoadChangelogConfiguration_WithMissingRequiredTypes_ReturnsError()
	{
		// Arrange
		var configLoader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config missing required type 'breaking-change'
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			""";
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var originalDir = FileSystem.Directory.GetCurrentDirectory();
		try
		{
			FileSystem.Directory.SetCurrentDirectory(configDir);

			// Act
			var config = await configLoader.LoadChangelogConfiguration(Collector, null, TestContext.Current.CancellationToken);

			// Assert
			config.Should().BeNull();
			Collector.Errors.Should().BeGreaterThan(0);
			Collector.Diagnostics.Should().Contain(d =>
				d.Severity == Severity.Error &&
				d.Message.Contains("Required type 'breaking-change' is missing"));
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task LoadChangelogConfiguration_WithSubtypesOnNonBreakingChange_ReturnsError()
	{
		// Arrange
		var configLoader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config with subtypes on a non-breaking-change type (feature)
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			      labels: ">feature"
			      subtypes:
			        api: ">api"
			    bug-fix:
			    breaking-change:
			""";
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var originalDir = FileSystem.Directory.GetCurrentDirectory();
		try
		{
			FileSystem.Directory.SetCurrentDirectory(configDir);

			// Act
			var config = await configLoader.LoadChangelogConfiguration(Collector, null, TestContext.Current.CancellationToken);

			// Assert
			config.Should().BeNull();
			Collector.Errors.Should().BeGreaterThan(0);
			Collector.Diagnostics.Should().Contain(d =>
				d.Severity == Severity.Error &&
				d.Message.Contains("Type 'feature' has subtypes defined") &&
				d.Message.Contains("subtypes are only allowed for 'breaking-change' type"));
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task LoadChangelogConfiguration_WithSubtypesOnBreakingChange_Succeeds()
	{
		// Arrange
		var configLoader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config with subtypes on breaking-change type (valid)
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			      labels: ">breaking"
			      subtypes:
			        api: ">api"
			        behavioral: ">behavioral"
			""";
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var originalDir = FileSystem.Directory.GetCurrentDirectory();
		try
		{
			FileSystem.Directory.SetCurrentDirectory(configDir);

			// Act
			var config = await configLoader.LoadChangelogConfiguration(Collector, null, TestContext.Current.CancellationToken);

			// Assert
			config.Should().NotBeNull();
			Collector.Errors.Should().Be(0);
			config!.Types.Should().Contain("breaking-change");
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task LoadChangelogConfiguration_WithInvalidSubtype_ReturnsError()
	{
		// Arrange
		var configLoader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config with invalid subtype value
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			      labels: ">breaking"
			      subtypes:
			        invalid-subtype: ">invalid"
			""";
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var originalDir = FileSystem.Directory.GetCurrentDirectory();
		try
		{
			FileSystem.Directory.SetCurrentDirectory(configDir);

			// Act
			var config = await configLoader.LoadChangelogConfiguration(Collector, null, TestContext.Current.CancellationToken);

			// Assert
			config.Should().BeNull();
			Collector.Errors.Should().BeGreaterThan(0);
			Collector.Diagnostics.Should().Contain(d =>
				d.Severity == Severity.Error &&
				d.Message.Contains("Subtype 'invalid-subtype'") &&
				d.Message.Contains("is not a valid subtype"));
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task LoadChangelogConfiguration_BlockCreate_AsString_ParsesCorrectly()
	{
		// Arrange - block.create as comma-separated string
		var config = await LoadConfig(
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			block:
			  create: ">non-issue, >test, >skip"
			""");

		// Assert
		config.Should().NotBeNull();
		Collector.Errors.Should().Be(0);
		config!.Block.Should().NotBeNull();
		config.Block!.Create.Should().BeEquivalentTo([">non-issue", ">test", ">skip"]);
	}

	[Fact]
	public async Task LoadChangelogConfiguration_BlockCreate_AsList_ParsesCorrectly()
	{
		// Arrange - block.create as YAML list
		var config = await LoadConfig(
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			block:
			  create:
			    - ">non-issue"
			    - ">test"
			    - ">skip"
			""");

		// Assert
		config.Should().NotBeNull();
		Collector.Errors.Should().Be(0);
		config!.Block.Should().NotBeNull();
		config.Block!.Create.Should().BeEquivalentTo([">non-issue", ">test", ">skip"]);
	}

	[Fact]
	public async Task LoadChangelogConfiguration_PublishBlockerTypes_AsString_ParsesCorrectly()
	{
		// Arrange - block.publish.types as comma-separated string
		var config = await LoadConfig(
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			block:
			  publish:
			    types: "deprecation, known-issue"
			""");

		// Assert
		config.Should().NotBeNull();
		Collector.Errors.Should().Be(0);
		config!.Block.Should().NotBeNull();
		config.Block!.Publish.Should().NotBeNull();
		config.Block.Publish!.Types.Should().BeEquivalentTo(["deprecation", "known-issue"]);
	}

	[Fact]
	public async Task LoadChangelogConfiguration_PublishBlockerTypes_AsList_ParsesCorrectly()
	{
		// Arrange - block.publish.types as YAML list
		var config = await LoadConfig(
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			block:
			  publish:
			    types:
			      - deprecation
			      - known-issue
			""");

		// Assert
		config.Should().NotBeNull();
		Collector.Errors.Should().Be(0);
		config!.Block.Should().NotBeNull();
		config.Block!.Publish.Should().NotBeNull();
		config.Block.Publish!.Types.Should().BeEquivalentTo(["deprecation", "known-issue"]);
	}

	[Fact]
	public async Task LoadChangelogConfiguration_PublishBlockerAreas_AsString_ParsesCorrectly()
	{
		// Arrange - block.publish.areas as comma-separated string
		var config = await LoadConfig(
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			block:
			  publish:
			    areas: "Internal, Experimental"
			""");

		// Assert
		config.Should().NotBeNull();
		Collector.Errors.Should().Be(0);
		config!.Block.Should().NotBeNull();
		config.Block!.Publish.Should().NotBeNull();
		config.Block.Publish!.Areas.Should().BeEquivalentTo(["Internal", "Experimental"]);
	}

	[Fact]
	public async Task LoadChangelogConfiguration_PublishBlockerAreas_AsList_ParsesCorrectly()
	{
		// Arrange - block.publish.areas as YAML list
		var config = await LoadConfig(
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			block:
			  publish:
			    areas:
			      - Internal
			      - Experimental
			""");

		// Assert
		config.Should().NotBeNull();
		Collector.Errors.Should().Be(0);
		config!.Block.Should().NotBeNull();
		config.Block!.Publish.Should().NotBeNull();
		config.Block.Publish!.Areas.Should().BeEquivalentTo(["Internal", "Experimental"]);
	}

	[Fact]
	public async Task LoadChangelogConfiguration_PivotHighlight_AsString_ParsesCorrectly()
	{
		// Arrange - pivot.highlight as comma-separated string
		var config = await LoadConfig(
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			  highlight: ">highlight, >release-highlight"
			""");

		// Assert
		config.Should().NotBeNull();
		Collector.Errors.Should().Be(0);
		config!.HighlightLabels.Should().BeEquivalentTo([">highlight", ">release-highlight"]);
	}

	[Fact]
	public async Task LoadChangelogConfiguration_PivotHighlight_AsList_ParsesCorrectly()
	{
		// Arrange - pivot.highlight as YAML list
		var config = await LoadConfig(
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			  highlight:
			    - ">highlight"
			    - ">release-highlight"
			""");

		// Assert
		config.Should().NotBeNull();
		Collector.Errors.Should().Be(0);
		config!.HighlightLabels.Should().BeEquivalentTo([">highlight", ">release-highlight"]);
	}

	[Fact]
	public async Task LoadChangelogConfiguration_PivotAreas_AsListValues_ComputesMapping()
	{
		// Arrange - pivot.areas with list values instead of comma-separated strings
		var config = await LoadConfig(
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			  areas:
			    Search:
			      - ":Search/Search"
			      - ":Search/Ranking"
			    Security: ":Security/Security"
			""");

		// Assert
		config.Should().NotBeNull();
		Collector.Errors.Should().Be(0);
		config!.Areas.Should().NotBeNull();
		config.Areas.Should().Contain("Search");
		config.Areas.Should().Contain("Security");
		// Both labels from the list should map to "Search"
		config.LabelToAreas.Should().NotBeNull();
		config.LabelToAreas.Should().ContainKey(":Search/Search");
		config.LabelToAreas![":Search/Search"].Should().Be("Search");
		config.LabelToAreas.Should().ContainKey(":Search/Ranking");
		config.LabelToAreas[":Search/Ranking"].Should().Be("Search");
		// String form should still work
		config.LabelToAreas.Should().ContainKey(":Security/Security");
		config.LabelToAreas[":Security/Security"].Should().Be("Security");
	}

	[Fact]
	public async Task LoadChangelogConfiguration_TypeLabels_AsList_ComputesMapping()
	{
		// Arrange - pivot.types labels as YAML list instead of comma-separated string
		var config = await LoadConfig(
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			      labels:
			        - ">bug"
			        - ">fix"
			    breaking-change:
			      labels:
			        - ">breaking"
			        - ">bc"
			""");

		// Assert
		config.Should().NotBeNull();
		Collector.Errors.Should().Be(0);
		config!.LabelToType.Should().NotBeNull();
		// bug-fix labels (list form)
		config.LabelToType.Should().ContainKey(">bug");
		config.LabelToType![">bug"].Should().Be("bug-fix");
		config.LabelToType.Should().ContainKey(">fix");
		config.LabelToType[">fix"].Should().Be("bug-fix");
		// breaking-change labels (list form)
		config.LabelToType.Should().ContainKey(">breaking");
		config.LabelToType[">breaking"].Should().Be("breaking-change");
		config.LabelToType.Should().ContainKey(">bc");
		config.LabelToType[">bc"].Should().Be("breaking-change");
	}

	[Fact]
	public async Task LoadChangelogConfiguration_SubtypeLabels_AsList_ParsesCorrectly()
	{
		// Arrange - breaking-change subtype labels as YAML list
		var config = await LoadConfig(
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			      labels: ">breaking"
			      subtypes:
			        api:
			          - ">api-breaking"
			          - ">api-change"
			        behavioral: ">behavioral-breaking"
			""");

		// Assert
		config.Should().NotBeNull();
		Collector.Errors.Should().Be(0);
		config!.Pivot.Should().NotBeNull();
		config.Pivot!.Types.Should().ContainKey("breaking-change");
		var breakingChange = config.Pivot.Types!["breaking-change"];
		breakingChange.Should().NotBeNull();
		breakingChange!.Subtypes.Should().NotBeNull();
		// List form subtype labels should be joined as comma-separated
		breakingChange.Subtypes!["api"].Should().Be(">api-breaking, >api-change");
		// String form should still work
		breakingChange.Subtypes["behavioral"].Should().Be(">behavioral-breaking");
	}

	[Fact]
	public async Task LoadChangelogConfiguration_ProductBlockCreate_AsList_ParsesCorrectly()
	{
		// Arrange - product-specific block.product.*.create as YAML list
		var config = await LoadConfig(
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			block:
			  product:
			    elasticsearch:
			      create:
			        - ">test"
			        - ">skip"
			""");

		// Assert
		config.Should().NotBeNull();
		Collector.Errors.Should().Be(0);
		config!.Block.Should().NotBeNull();
		config.Block!.ByProduct.Should().NotBeNull();
		config.Block.ByProduct.Should().ContainKey("elasticsearch");
		config.Block.ByProduct!["elasticsearch"].Create.Should().BeEquivalentTo([">test", ">skip"]);
	}

	[Fact]
	public async Task LoadChangelogConfiguration_MixedStringAndListForms_ParsesCorrectly()
	{
		// Arrange - mix of string and list forms in the same config
		var config = await LoadConfig(
			"""
			pivot:
			  types:
			    feature:
			    bug-fix: ">bug"
			    breaking-change:
			      labels:
			        - ">breaking"
			        - ">bc"
			      subtypes:
			        api: ">api-breaking"
			        behavioral:
			          - ">behavioral-breaking"
			  areas:
			    Search: ":Search/Search, :Search/Ranking"
			    Security:
			      - ":Security/Security"
			      - ":Security/Auth"
			  highlight:
			    - ">highlight"
			block:
			  create: ">non-issue, >test"
			  publish:
			    types: "deprecation, known-issue"
			    areas:
			      - Internal
			""");

		// Assert
		config.Should().NotBeNull();
		Collector.Errors.Should().Be(0);

		// block.create as string
		config!.Block.Should().NotBeNull();
		config.Block!.Create.Should().BeEquivalentTo([">non-issue", ">test"]);

		// publish.types as string, publish.areas as list
		config.Block.Publish.Should().NotBeNull();
		config.Block.Publish!.Types.Should().BeEquivalentTo(["deprecation", "known-issue"]);
		config.Block.Publish.Areas.Should().BeEquivalentTo(["Internal"]);

		// highlight as list
		config.HighlightLabels.Should().BeEquivalentTo([">highlight"]);

		// Type labels: string for bug-fix, list for breaking-change
		config.LabelToType.Should().ContainKey(">bug");
		config.LabelToType![">bug"].Should().Be("bug-fix");
		config.LabelToType.Should().ContainKey(">breaking");
		config.LabelToType[">breaking"].Should().Be("breaking-change");

		// Areas: string for Search, list for Security
		config.LabelToAreas.Should().ContainKey(":Search/Search");
		config.LabelToAreas![":Search/Search"].Should().Be("Search");
		config.LabelToAreas.Should().ContainKey(":Security/Security");
		config.LabelToAreas[":Security/Security"].Should().Be("Security");
		config.LabelToAreas.Should().ContainKey(":Security/Auth");
		config.LabelToAreas[":Security/Auth"].Should().Be("Security");
	}

	/// <summary>
	/// Helper to reduce boilerplate in lenient list tests.
	/// Creates a temporary config file and loads the configuration.
	/// </summary>
	private async Task<ChangelogConfiguration?> LoadConfig(string yamlContent)
	{
		var configLoader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		await FileSystem.File.WriteAllTextAsync(configPath, yamlContent, TestContext.Current.CancellationToken);

		var originalDir = FileSystem.Directory.GetCurrentDirectory();
		try
		{
			FileSystem.Directory.SetCurrentDirectory(configDir);
			return await configLoader.LoadChangelogConfiguration(Collector, null, TestContext.Current.CancellationToken);
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}
}
