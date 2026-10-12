// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.FileSystems;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// The unified release command checks <c>--output-dir</c> against the scope of the changelog file system
/// up front, so a folder outside it fails with a clear message instead of "config file not found".
/// </summary>
public class ChangelogFileSystemScopeTests
{
	private static readonly string Root = Path.Join(Path.GetFullPath(Directory.GetCurrentDirectory()), "scope-root");

	[Test]
	public void IsWithin_FolderBelowTheRoot_ReturnsTrue() =>
		ChangelogFileSystem.IsWithin(Root, Path.Join(Root, "bundles", "kibana")).Should().BeTrue();

	[Test]
	public void IsWithin_TheRootItself_ReturnsTrue() => ChangelogFileSystem.IsWithin(Root, Root).Should().BeTrue();

	[Test]
	public void IsWithin_RootWithTrailingSeparator_ReturnsTrue() =>
		ChangelogFileSystem.IsWithin(Root, Root + Path.DirectorySeparatorChar).Should().BeTrue();

	[Test]
	public void IsWithin_SiblingThatSharesTheRootNamePrefix_ReturnsFalse() =>
		ChangelogFileSystem.IsWithin(Root, Root + "-other").Should().BeFalse();

	[Test]
	public void IsWithin_ParentOfTheRoot_ReturnsFalse() =>
		ChangelogFileSystem.IsWithin(Root, Path.GetDirectoryName(Root)!).Should().BeFalse();

	[Test]
	public void IsWithin_PathThatEscapesThroughDotDot_ReturnsFalse() =>
		ChangelogFileSystem.IsWithin(Root, Path.Join(Root, "bundles", "..", "..", "elsewhere")).Should().BeFalse();
}
