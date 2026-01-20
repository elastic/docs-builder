// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using FluentAssertions;

namespace Elastic.Documentation.Services.Tests.Changelogs;

public class ChangelogConfigurationTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	[Fact]
	public async Task LoadChangelogConfiguration_WithoutAvailableTypes_UsesDefaults()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null, _fileSystem);
		var configDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = _fileSystem.Path.Combine(configDir, "docs");
		_fileSystem.Directory.CreateDirectory(docsDir);
		var configPath = _fileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config without available_types - should use defaults
		// language=yaml
		var configContent =
			"""
			available_subtypes: []
			available_lifecycles:
			  - ga
			""";
		await _fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var originalDir = _fileSystem.Directory.GetCurrentDirectory();
		try
		{
			_fileSystem.Directory.SetCurrentDirectory(configDir);

			// Act
			var config = await service.LoadChangelogConfiguration(_collector, null, TestContext.Current.CancellationToken);

			// Assert
			config.Should().NotBeNull();
			_collector.Errors.Should().Be(0);
			// Should have default types
			config.AvailableTypes.Should().Contain("feature");
			config.AvailableTypes.Should().Contain("bug-fix");
			config.AvailableTypes.Should().Contain("docs");
		}
		finally
		{
			_fileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task LoadChangelogConfiguration_WithoutAvailableSubtypes_UsesDefaults()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null, _fileSystem);
		var configDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = _fileSystem.Path.Combine(configDir, "docs");
		_fileSystem.Directory.CreateDirectory(docsDir);
		var configPath = _fileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config without available_subtypes - should use defaults
		// language=yaml
		var configContent =
			"""
			available_types:
			  - feature
			available_lifecycles:
			   ga
			""";
		await _fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var originalDir = _fileSystem.Directory.GetCurrentDirectory();
		try
		{
			_fileSystem.Directory.SetCurrentDirectory(configDir);

			// Act
			var config = await service.LoadChangelogConfiguration(_collector, null, TestContext.Current.CancellationToken);

			// Assert
			config.Should().NotBeNull();
			_collector.Errors.Should().Be(0);
			// Should have default subtypes
			config.AvailableSubtypes.Should().Contain("api");
			config.AvailableSubtypes.Should().Contain("behavioral");
		}
		finally
		{
			_fileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task LoadChangelogConfiguration_WithoutAvailableLifecycles_UsesDefaults()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null, _fileSystem);
		var configDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = _fileSystem.Path.Combine(configDir, "docs");
		_fileSystem.Directory.CreateDirectory(docsDir);
		var configPath = _fileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config without available_lifecycles - should use defaults
		// language=yaml
		var configContent =
			"""
			available_types:
			  - feature
			available_subtypes: []
			""";
		await _fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var originalDir = _fileSystem.Directory.GetCurrentDirectory();
		try
		{
			_fileSystem.Directory.SetCurrentDirectory(configDir);

			// Act
			var config = await service.LoadChangelogConfiguration(_collector, null, TestContext.Current.CancellationToken);

			// Assert
			config.Should().NotBeNull();
			_collector.Errors.Should().Be(0);
			// Should have default lifecycles
			config.AvailableLifecycles.Should().Contain("preview");
			config.AvailableLifecycles.Should().Contain("beta");
			config.AvailableLifecycles.Should().Contain("ga");
		}
		finally
		{
			_fileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task LoadChangelogConfiguration_WithInvalidRenderBlockersType_ReturnsError()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null, _fileSystem);
		var configDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = _fileSystem.Path.Combine(configDir, "docs");
		_fileSystem.Directory.CreateDirectory(docsDir);
		var configPath = _fileSystem.Path.Combine(docsDir, "changelog.yml");
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
		await _fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var originalDir = _fileSystem.Directory.GetCurrentDirectory();
		try
		{
			_fileSystem.Directory.SetCurrentDirectory(configDir);

			// Act
			var config = await service.LoadChangelogConfiguration(_collector, null, TestContext.Current.CancellationToken);

			// Assert
			config.Should().BeNull();
			_collector.Errors.Should().BeGreaterThan(0);
			_collector.Diagnostics.Should().Contain(d =>
				d.Severity == Severity.Error &&
				d.Message.Contains("Type 'invalid-type' in render_blockers") &&
				d.Message.Contains("is not in the list of available types"));
		}
		finally
		{
			_fileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}
}
