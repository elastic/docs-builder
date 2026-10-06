// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Codex;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.FileSystems;

namespace Elastic.Documentation.Navigation.Tests.Codex;

public class CodexConfigurationLoaderTests()
{
	private const string ValidConfig = """
		environment: internal
		site_prefix: /
		groups: []
		""";

	private static readonly string ConfigPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "codex.yml");

	private CheckoutsFileSystem ScopedFs(MockFileSystem mockFs) => CheckoutsFileSystem.FromWorkingDirectory(mockFs);

	private TestDiagnosticsCollector Collector() => new();

	[Test]
	public void Load_FileNotFound_ReturnsFalseWithError()
	{
		var fs = ScopedFs(new MockFileSystem());
		var configFile = fs.FileInfo.New(ConfigPath);
		var collector = Collector();

		var result = CodexConfigurationLoader.Load(configFile, ConfigPath, collector, CodexLoadPurpose.Publish, out _);

		result.Should().BeFalse();
		collector.Errors.Should().Be(1);
		collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("not found"));
	}

	[Test]
	public void Load_MissingEnvironmentField_ReturnsFalseWithError()
	{
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ ConfigPath, new MockFileData("site_prefix: /\ngroups: []") }
		});
		var fs = ScopedFs(mockFs);
		var configFile = fs.FileInfo.New(ConfigPath);
		var collector = Collector();

		var result = CodexConfigurationLoader.Load(configFile, ConfigPath, collector, CodexLoadPurpose.Publish, out _);

		result.Should().BeFalse();
		collector.Errors.Should().Be(1);
		collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("environment"));
	}

	[Test]
	public void Load_ValidConfig_ReturnsTrueAndEnvironment()
	{
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData> { { ConfigPath, new MockFileData(ValidConfig) } });
		var fs = ScopedFs(mockFs);
		var configFile = fs.FileInfo.New(ConfigPath);
		var collector = Collector();

		var result = CodexConfigurationLoader.Load(configFile, ConfigPath, collector, CodexLoadPurpose.Publish, out var config);

		result.Should().BeTrue();
		collector.Errors.Should().Be(0);
		config.Should().NotBeNull();
		config.Environment.Should().Be("internal");
	}

	[Test]
	public void Load_NoEnvironmentRequired_LoadsSuccessfullyWithoutEnvironment()
	{
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ ConfigPath, new MockFileData("site_prefix: /\ngroups: []") }
		});
		var fs = ScopedFs(mockFs);
		var configFile = fs.FileInfo.New(ConfigPath);
		var collector = Collector();

		var result = CodexConfigurationLoader.Load(configFile, ConfigPath, collector, CodexLoadPurpose.Read, out var config);

		result.Should().BeTrue();
		collector.Errors.Should().Be(0);
		config.Should().NotBeNull();
	}

	[Test]
	public void Load_MissingEnvironmentForRead_IsNotReportedAndPublishReportsItOnce()
	{
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ ConfigPath, new MockFileData("site_prefix: /\ngroups: []") }
		});
		var fs = ScopedFs(mockFs);
		var configFile = fs.FileInfo.New(ConfigPath);
		var read = Collector();
		var publish = Collector();

		CodexConfigurationLoader.Load(configFile, ConfigPath, read, CodexLoadPurpose.Read, out _).Should().BeTrue();
		CodexConfigurationLoader.Load(configFile, ConfigPath, publish, CodexLoadPurpose.Publish, out _).Should().BeFalse();

		read.Diagnostics.Should().BeEmpty();
		publish.Diagnostics.Should().ContainSingle(d => d.RuleId == "environment-required");
	}

	[Test]
	public void Load_ScopedFileSystemOutOfScope_ReturnsFalseWithError()
	{
		// Simulate the real bug: config lives outside the scoped filesystem.
		// ScopedFileInfo.Exists returns true (unguarded), but OpenText throws.
		// Load must catch that and emit a visible error instead of propagating.
		var outsidePath = Path.Join(Path.GetTempPath(), "codex.yml");
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData> { { outsidePath, new MockFileData(ValidConfig) } });
		// Scope the FS only to the working dir — the outsidePath is outside it
		var fs = ScopedFs(mockFs);
		var configFile = fs.FileInfo.New(outsidePath);
		var collector = Collector();

		var result = CodexConfigurationLoader.Load(configFile, outsidePath, collector, CodexLoadPurpose.Publish, out _);

		result.Should().BeFalse();
		collector.Errors.Should().Be(1);
	}
}
