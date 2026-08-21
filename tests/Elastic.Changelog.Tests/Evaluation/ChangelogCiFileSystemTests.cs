// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.FileSystems;
using Nullean.ScopedFileSystem;

namespace Elastic.Changelog.Tests.Evaluation;

/// <summary>
/// Regression: changelog CI staging uses <c>.artifacts/changelog-*</c> under the checkout.
/// ScopedFileSystem rejects hidden path segments unless they are allowlisted.
/// </summary>
public class ChangelogCiFileSystemTests
{
	private static readonly string Root = Paths.WorkingDirectoryRoot.FullName;

	[Fact]
	public void ChangelogFileSystem_AllowsArtifactsStagingAndArtifactDirs()
	{
		var mock = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = Root });
		var fs = ChangelogFileSystem.FromWorkingDirectory(mock);

		var staging = Path.Join(Root, ".artifacts", "changelog-staging");
		var artifact = Path.Join(Root, ".artifacts", "changelog-artifact");
		var stagingFile = Path.Join(staging, "42.yaml");
		var artifactFile = Path.Join(artifact, "metadata.json");

		var create = () =>
		{
			fs.Directory.CreateDirectory(staging);
			fs.Directory.CreateDirectory(artifact);
			fs.File.WriteAllText(stagingFile, "title: test");
			fs.File.WriteAllText(artifactFile, "{}");
		};

		create.Should().NotThrow();
		fs.File.Exists(stagingFile).Should().BeTrue();
		fs.File.Exists(artifactFile).Should().BeTrue();
	}

	[Fact]
	public void ChangelogFileSystem_BlocksOtherHiddenDirectories()
	{
		var mock = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = Root });
		var fs = ChangelogFileSystem.FromWorkingDirectory(mock);
		var hidden = Path.Join(Root, ".hidden", "nested");

		var create = () => fs.Directory.CreateDirectory(hidden);

		create.Should().Throw<ScopedFileSystemException>()
			.WithMessage("*hidden*");
	}

	[Fact]
	public void RunnerTempFileSystem_AllowsArtifactsStagingAndArtifactDirs()
	{
		var mock = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = Root });
		var fs = new RunnerTempFileSystem(mock.DirectoryInfo.New(Root), inner: mock);

		var staging = Path.Join(Root, ".artifacts", "changelog-staging");
		var artifact = Path.Join(Root, ".artifacts", "changelog-artifact");
		var stagingFile = Path.Join(staging, "42.yaml");
		var artifactFile = Path.Join(artifact, "metadata.json");

		var create = () =>
		{
			fs.Directory.CreateDirectory(staging);
			fs.Directory.CreateDirectory(artifact);
			fs.File.WriteAllText(stagingFile, "title: test");
			fs.File.WriteAllText(artifactFile, "{}");
		};

		create.Should().NotThrow();
		fs.File.Exists(stagingFile).Should().BeTrue();
		fs.File.Exists(artifactFile).Should().BeTrue();
	}

	[Fact]
	public void RunnerTempFileSystem_BlocksOtherHiddenDirectories()
	{
		var mock = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = Root });
		var fs = new RunnerTempFileSystem(mock.DirectoryInfo.New(Root), inner: mock);
		var hidden = Path.Join(Root, ".hidden", "nested");

		var create = () => fs.Directory.CreateDirectory(hidden);

		create.Should().Throw<ScopedFileSystemException>()
			.WithMessage("*hidden*");
	}
}
