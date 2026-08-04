// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Codex;
using Elastic.Documentation.Configuration;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Navigation.Tests.Codex;

public class CodexConfigurationLoaderTests(ITestOutputHelper output)
{
	private const string ValidConfig = """
		environment: internal
		site_prefix: /
		groups: []
		""";

	private static readonly string ConfigPath =
		Path.Join(Paths.WorkingDirectoryRoot.FullName, "codex.yml");

	private ScopedFileSystem ScopedFs(MockFileSystem mockFs) =>
		FileSystemFactory.ScopeCurrentWorkingDirectory(mockFs);

	private TestDiagnosticsCollector Collector() => new(output);

	[Fact]
	public void TryLoad_FileNotFound_ReturnsFalseWithError()
	{
		var fs = ScopedFs(new MockFileSystem());
		var configFile = fs.FileInfo.New(ConfigPath);
		var collector = Collector();

		var result = CodexConfigurationLoader.TryLoad(configFile, ConfigPath, collector, out _, out _);

		result.Should().BeFalse();
		collector.Errors.Should().Be(1);
		collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("not found"));
	}

	[Fact]
	public void TryLoad_MissingEnvironmentField_ReturnsFalseWithError()
	{
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ ConfigPath, new MockFileData("site_prefix: /\ngroups: []") }
		});
		var fs = ScopedFs(mockFs);
		var configFile = fs.FileInfo.New(ConfigPath);
		var collector = Collector();

		var result = CodexConfigurationLoader.TryLoad(configFile, ConfigPath, collector, out _, out _);

		result.Should().BeFalse();
		collector.Errors.Should().Be(1);
		collector.Diagnostics.Should().ContainSingle(d => d.Message.Contains("environment"));
	}

	[Fact]
	public void TryLoad_ValidConfig_ReturnsTrueAndEnvironment()
	{
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ ConfigPath, new MockFileData(ValidConfig) }
		});
		var fs = ScopedFs(mockFs);
		var configFile = fs.FileInfo.New(ConfigPath);
		var collector = Collector();

		var result = CodexConfigurationLoader.TryLoad(configFile, ConfigPath, collector, out var config, out var environment);

		result.Should().BeTrue();
		collector.Errors.Should().Be(0);
		config.Should().NotBeNull();
		environment.Should().Be("internal");
	}

	[Fact]
	public void TryLoad_NoEnvironmentRequired_LoadsSuccessfullyWithoutEnvironment()
	{
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ ConfigPath, new MockFileData("site_prefix: /\ngroups: []") }
		});
		var fs = ScopedFs(mockFs);
		var configFile = fs.FileInfo.New(ConfigPath);
		var collector = Collector();

		var result = CodexConfigurationLoader.TryLoad(configFile, ConfigPath, collector, out var config);

		result.Should().BeTrue();
		collector.Errors.Should().Be(0);
		config.Should().NotBeNull();
	}

	[Fact]
	public void TryLoad_ScopedFileSystemOutOfScope_ReturnsFalseWithError()
	{
		// Simulate the real bug: config lives outside the scoped filesystem.
		// ScopedFileInfo.Exists returns true (unguarded), but OpenText throws.
		// TryLoad must catch that and emit a visible error instead of propagating.
		var outsidePath = Path.Join(Path.GetTempPath(), "codex.yml");
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ outsidePath, new MockFileData(ValidConfig) }
		});
		// Scope the FS only to the working dir — the outsidePath is outside it
		var fs = ScopedFs(mockFs);
		var configFile = fs.FileInfo.New(outsidePath);
		var collector = Collector();

		var result = CodexConfigurationLoader.TryLoad(configFile, outsidePath, collector, out _, out _);

		result.Should().BeFalse();
		collector.Errors.Should().Be(1);
	}
}
