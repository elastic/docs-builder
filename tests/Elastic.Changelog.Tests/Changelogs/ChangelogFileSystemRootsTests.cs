// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.FileSystems;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Changelog commands read from the git root and may write to an output folder elsewhere, like the
/// documentation build. Only the folders they were told about are reachable.
/// </summary>
public class ChangelogFileSystemRootsTests
{
	private static readonly string Repository = Paths.WorkingDirectoryRoot.FullName;
	private static readonly string Outside = Path.Join(Path.GetDirectoryName(Repository)!, "bundles-elsewhere");
	private static readonly string Unrelated = Path.Join(Path.GetDirectoryName(Repository)!, "unrelated-folder");

	private static (MockFileSystem Mock, ChangelogFileSystem Scoped) Create(params string[] extraRoots)
	{
		var mock = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = Repository });
		mock.Directory.CreateDirectory(Outside);
		mock.Directory.CreateDirectory(Unrelated);
		return (mock, new ChangelogFileSystem(mock.DirectoryInfo.New(Repository), mock, extraRoots));
	}

	[Test]
	public void FolderOutsideTheRepository_WithoutAnExtraRoot_IsBlocked()
	{
		var (_, scoped) = Create();

		var write = () => scoped.File.WriteAllText(Path.Join(Outside, "bundle.yaml"), "x");

		write.Should().Throw<Exception>();
	}

	[Test]
	public void ExtraRootOutsideTheRepository_AllowsWritingAndReadingBack()
	{
		var (mock, scoped) = Create(Outside);
		var file = Path.Join(Outside, "kibana", "bundle.yaml");
		scoped.Directory.CreateDirectory(Path.GetDirectoryName(file)!);

		scoped.File.WriteAllText(file, "bundle: true");

		scoped.File.ReadAllText(file).Should().Be("bundle: true");
		mock.File.Exists(file).Should().BeTrue();
	}

	[Test]
	public void ExtraRootOutsideTheRepository_DoesNotOpenAnyOtherFolder()
	{
		var (_, scoped) = Create(Outside);

		var write = () => scoped.File.WriteAllText(Path.Join(Unrelated, "bundle.yaml"), "x");

		write.Should().Throw<Exception>();
	}

	[Test]
	public void ExtraRootOutsideTheRepository_KeepsTheRepositoryReachable()
	{
		var (_, scoped) = Create(Outside);
		var file = Path.Join(Repository, "docs", "changelog.yml");
		scoped.Directory.CreateDirectory(Path.GetDirectoryName(file)!);

		scoped.File.WriteAllText(file, "bundle: {}");

		scoped.File.ReadAllText(file).Should().Be("bundle: {}");
	}

	[Test]
	public void ExtraRootInsideTheRepository_ChangesNothing()
	{
		var (_, scoped) = Create(Path.Join(Repository, "bundles"));
		var file = Path.Join(Repository, "bundles", "kibana", "bundle.yaml");
		scoped.Directory.CreateDirectory(Path.GetDirectoryName(file)!);

		scoped.File.WriteAllText(file, "bundle: true");

		scoped.File.Exists(file).Should().BeTrue();
		var elsewhere = () => scoped.File.WriteAllText(Path.Join(Unrelated, "bundle.yaml"), "x");
		elsewhere.Should().Throw<Exception>();
	}
}
