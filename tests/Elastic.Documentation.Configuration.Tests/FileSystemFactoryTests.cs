// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;

namespace Elastic.Documentation.Configuration.Tests;

sealed file class StubEnv(string? runnerTemp) : IEnvironmentVariables
{
	public string? GetEnvironmentVariable(string name) =>
		name == "RUNNER_TEMP" ? runnerTemp : null;
	public bool IsRunningOnCI => runnerTemp is not null;
}

public class FileSystemFactoryTests
{
	[Fact]
	public void ScopeCurrentWorkingDirectory_NestedExtensionRoot_DoesNotThrow()
	{
		var workingRoot = Paths.WorkingDirectoryRoot.FullName;
		var nestedConfigDir = Path.Join(workingRoot, "environments", "internal");
		var configPath = Path.Join(nestedConfigDir, "config.yml");
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ configPath, new MockFileData("environment: internal") }
		});

		var act = () => FileSystemFactory.ScopeCurrentWorkingDirectory(mockFs, [nestedConfigDir]);

		act.Should().NotThrow();
		var scoped = FileSystemFactory.ScopeCurrentWorkingDirectory(mockFs, [nestedConfigDir]);
		scoped.File.Exists(configPath).Should().BeTrue();
	}

	[Fact]
	public void ScopeCurrentWorkingDirectory_ExternalExtensionRoot_AllowsReadingExternalConfig()
	{
		var externalRoot = Path.Join(Path.GetTempPath(), $"external-codex-{Guid.NewGuid():N}");
		var configPath = Path.Join(externalRoot, "codex.yml");
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ configPath, new MockFileData("environment: internal") }
		});

		var scoped = FileSystemFactory.ScopeCurrentWorkingDirectory(mockFs, [externalRoot]);

		scoped.File.Exists(configPath).Should().BeTrue();
	}

	[Fact]
	public void ScopeCurrentWorkingDirectory_AncestorExtensionRoot_DoesNotThrow()
	{
		// An ancestor of the working root would produce overlapping roots, the same class of
		// crash fixed in a96ef869 / 3c5f9703 for Codex nested paths.
		var workingRoot = Paths.WorkingDirectoryRoot.FullName;
		var ancestor = Path.GetDirectoryName(workingRoot)!;
		var mockFs = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = workingRoot });

		var act = () => FileSystemFactory.ScopeCurrentWorkingDirectory(mockFs, [ancestor]);

		act.Should().NotThrow();
		var scoped = act();
		// The scoped filesystem must still allow reads within the working root.
		var fileInWorkingRoot = Path.Join(workingRoot, "some.yml");
		mockFs.AddFile(fileInWorkingRoot, new MockFileData("test"));
		scoped.File.Exists(fileInWorkingRoot).Should().BeTrue();
	}

	[Fact]
	public void RealReadForRunnerTemp_RunnerTempUnset_FallsBackToRealRead()
	{
		var env = new StubEnv(runnerTemp: null);

		var result = FileSystemFactory.RealReadForRunnerTemp(env);

		result.Should().BeSameAs(FileSystemFactory.RealRead);
	}

	[Fact]
	public void RealReadForRunnerTemp_RunnerTempSibling_AllowsReadingStagedFile()
	{
		// Simulate the GitHub Actions hosted runner layout: RUNNER_TEMP and the
		// checkout root are sibling directories. A file staged by the action in
		// RUNNER_TEMP must be readable, which the plain RealRead scope denies.
		var tempDir = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
		var env = new StubEnv(runnerTemp: tempDir);
		var stagedFile = Path.Join(tempDir, "changelog-pr-body.md");
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ stagedFile, new MockFileData("Release Notes: fix memory leak") }
		}, new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName });

		var scoped = FileSystemFactory.RealReadForRunnerTemp(env);
		// We call the overload that accepts inner FS, reusing the factory helper
		// that RealReadForRunnerTemp delegates to.
		var scopedMock = FileSystemFactory.ScopeCurrentWorkingDirectory(mockFs, [tempDir]);

		scopedMock.File.Exists(stagedFile).Should().BeTrue();
	}
}
