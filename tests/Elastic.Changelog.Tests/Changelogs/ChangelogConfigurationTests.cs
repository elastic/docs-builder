// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Configuration;
using Elastic.Changelog.Serialization;
using Elastic.Documentation;
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
}
