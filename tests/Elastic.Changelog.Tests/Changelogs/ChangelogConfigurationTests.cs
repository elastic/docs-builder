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
	public async Task LoadChangelogConfiguration_WithoutAvailableTypes_UsesDefaults()
	{
		// Arrange
		var configLoader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config without available_types - should use defaults
		// language=yaml
		var configContent =
			"""
			available_subtypes: []
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
			config.AvailableTypes.Should().Contain("feature");
			config.AvailableTypes.Should().Contain("bug-fix");
			config.AvailableTypes.Should().Contain("docs");
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task LoadChangelogConfiguration_WithoutAvailableSubtypes_UsesDefaults()
	{
		// Arrange
		var configLoader = new ChangelogConfigurationLoader(LoggerFactory, ConfigurationContext, FileSystem);
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config without available_subtypes - should use defaults
		// language=yaml
		var configContent =
			"""
			available_types:
			  - feature
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
			// Should have default subtypes
			config.AvailableSubtypes.Should().Contain("api");
			config.AvailableSubtypes.Should().Contain("behavioral");
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
			available_types:
			  - feature
			available_subtypes: []
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
			config.AvailableLifecycles.Should().Contain("preview");
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
			available_types:
			  - feature
			  - docs
			available_subtypes: []
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
}
