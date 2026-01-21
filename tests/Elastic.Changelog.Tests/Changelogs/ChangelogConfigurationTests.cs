// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Configuration;
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
			available_lifecycles:
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
			config!.AvailableTypes.Should().Contain("feature");
			config.AvailableTypes.Should().Contain("bug-fix");
			config.AvailableTypes.Should().Contain("docs");
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
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature: ">feature"
			    bug-fix: ">bug"
			available_lifecycles:
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
			config!.AvailableSubtypes.Should().Contain("api");
			config.AvailableSubtypes.Should().Contain("behavioral");
			// Should have types from pivot.types keys
			config.AvailableTypes.Should().Contain("feature");
			config.AvailableTypes.Should().Contain("bug-fix");
			config.AvailableTypes.Should().HaveCount(2);
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
		// Config without available_lifecycles - should use defaults
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
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
			// Should have default lifecycles
			config!.AvailableLifecycles.Should().Contain("preview");
			config.AvailableLifecycles.Should().Contain("beta");
			config.AvailableLifecycles.Should().Contain("ga");
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task LoadChangelogConfiguration_WithInvalidRenderBlockersType_ReturnsError()
	{
		// Arrange
		var configLoader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config with invalid type in render_blockers
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    docs:
			available_lifecycles:
			  - ga
			render_blockers:
			  elasticsearch:
			    types:
			      - invalid-type
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
				d.Message.Contains("Type 'invalid-type' in render_blockers") &&
				d.Message.Contains("is not in the list of available types"));
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
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
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
			config!.AvailableAreas.Should().NotBeNull();
			config.AvailableAreas.Should().Contain("Search");
			config.AvailableAreas.Should().Contain("Security");
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
				d.Message.Contains("is not in the list of available types"));
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}
}
