// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.FileSystems;

namespace Elastic.Documentation.Configuration.Tests;

public class CheckoutsFileSystemTests
{
	[Fact]
	public void NestedExtensionRoot_DoesNotThrow()
	{
		var workingRoot = Paths.WorkingDirectoryRoot.FullName;
		var nestedConfigDir = Path.Join(workingRoot, "environments", "internal");
		var configPath = Path.Join(nestedConfigDir, "config.yml");
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ configPath, new MockFileData("environment: internal") }
		});

		var act = () => new CheckoutsFileSystem(mockFs.DirectoryInfo.New(workingRoot), inner: mockFs, extraRoots: [nestedConfigDir]);

		act.Should().NotThrow();
		var scoped = act();
		scoped.File.Exists(configPath).Should().BeTrue();
	}

	[Fact]
	public void ExternalExtensionRoot_AllowsReadingExternalConfig()
	{
		var workingRoot = Paths.WorkingDirectoryRoot.FullName;
		var externalRoot = Path.Join(Path.GetTempPath(), $"external-codex-{Guid.NewGuid():N}");
		var configPath = Path.Join(externalRoot, "codex.yml");
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ configPath, new MockFileData("environment: internal") }
		});

		var scoped = new CheckoutsFileSystem(mockFs.DirectoryInfo.New(workingRoot), inner: mockFs, extraRoots: [externalRoot]);

		scoped.File.Exists(configPath).Should().BeTrue();
	}

	[Fact]
	public void AncestorExtensionRoot_DoesNotThrow()
	{
		// An ancestor of the working root would produce overlapping roots, the same class of
		// crash fixed in a96ef869 / 3c5f9703 for Codex nested paths.
		var workingRoot = Paths.WorkingDirectoryRoot.FullName;
		var ancestor = Path.GetDirectoryName(workingRoot)!;
		var mockFs = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = workingRoot });

		var act = () => new CheckoutsFileSystem(mockFs.DirectoryInfo.New(workingRoot), inner: mockFs, extraRoots: [ancestor]);

		act.Should().NotThrow();
		var scoped = act();
		var fileInWorkingRoot = Path.Join(workingRoot, "some.yml");
		mockFs.AddFile(fileInWorkingRoot, new MockFileData("test"));
		scoped.File.Exists(fileInWorkingRoot).Should().BeTrue();
	}

	[Fact]
	public void ExtraRunnerTempRoot_AllowsReadingStagedFile()
	{
		// Simulate the GitHub Actions hosted runner layout: RUNNER_TEMP and the checkout root
		// are sibling directories. A file staged in RUNNER_TEMP must be readable, which a
		// plain working-dir scope denies.
		var tempDir = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
		var stagedFile = Path.Join(tempDir, "changelog-pr-body.md");
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ stagedFile, new MockFileData("Release Notes: fix memory leak") }
		}, new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName });

		var scoped = new CheckoutsFileSystem(mockFs.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName), inner: mockFs, extraRoots: [tempDir]);

		scoped.File.Exists(stagedFile).Should().BeTrue();
	}
}
