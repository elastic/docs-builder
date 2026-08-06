// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Refactor.Tracking;

namespace Elastic.Markdown.Tests.Tracking;

/// <summary>
/// Tests for <see cref="IntegrationGitRepositoryTracker"/>, which reads the
/// ADDED_FILES / MODIFIED_FILES / DELETED_FILES / RENAMED_FILES environment
/// variables that the docs-build workflow exposes on CI.
/// </summary>
[Collection(TrackingTestCollection.Name)]
public sealed class IntegrationGitRepositoryTrackerTests : IDisposable
{
	private static readonly string[] EnvVarNames =
	[
		"ADDED_FILES",
		"MODIFIED_FILES",
		"DELETED_FILES",
		"RENAMED_FILES"
	];

	public IntegrationGitRepositoryTrackerTests() => ClearEnv();

	public void Dispose() => ClearEnv();

	private static void ClearEnv()
	{
		foreach (var name in EnvVarNames)
			Environment.SetEnvironmentVariable(name, null);
	}

	private static (IDirectoryInfo GitRoot, IDirectoryInfo Docset) CreateDirs(string? docsetRelative = "docs")
	{
		var fs = new MockFileSystem();
		var gitRoot = fs.DirectoryInfo.New("/repo");
		var docset = docsetRelative is null or "" or "."
			? gitRoot
			: fs.DirectoryInfo.New(fs.Path.Join(gitRoot.FullName, docsetRelative));
		return (gitRoot, docset);
	}

	[Fact]
	public void DocsetUnderSubfolder_FiltersPathsByPrefix()
	{
		Environment.SetEnvironmentVariable("DELETED_FILES", "docs/foo.md docs-extra/bar.md other/baz.md");

		var (gitRoot, docset) = CreateDirs("docs");
		var tracker = new IntegrationGitRepositoryTracker(gitRoot, docset);

		var changes = tracker.GetChangedFiles();

		changes.Should().ContainSingle()
			.Which.Should().BeEquivalentTo(new GitChange("docs/foo.md", GitChangeType.Deleted));
	}

	[Fact]
	public void DocsetAtRepoRoot_DotLookupPath_ReturnsAllChanges()
	{
		// Regression test for https://github.com/elastic/docs-content/pull/6479:
		// when the docset lives at the repo root (e.g. docs-content), every path
		// under the git root must be included — previously a "./" string prefix
		// silently dropped every file from the env vars.
		Environment.SetEnvironmentVariable("DELETED_FILES", "troubleshoot/deployments/serverless.md");
		Environment.SetEnvironmentVariable("MODIFIED_FILES", "troubleshoot/toc.yml");

		var (gitRoot, docset) = CreateDirs(".");
		var tracker = new IntegrationGitRepositoryTracker(gitRoot, docset);

		var changes = tracker.GetChangedFiles();

		changes.Should().BeEquivalentTo(
		[
			new GitChange("troubleshoot/deployments/serverless.md", GitChangeType.Deleted),
			new GitChange("troubleshoot/toc.yml", GitChangeType.Modified)
		]);
	}

	[Fact]
	public void DocsetAtRepoRoot_EmptyLookupPath_ReturnsAllChanges()
	{
		Environment.SetEnvironmentVariable("ADDED_FILES", "a.md b.md");

		var (gitRoot, docset) = CreateDirs("");
		var tracker = new IntegrationGitRepositoryTracker(gitRoot, docset);

		var changes = tracker.GetChangedFiles();

		changes.Should().BeEquivalentTo(
		[
			new GitChange("a.md", GitChangeType.Added),
			new GitChange("b.md", GitChangeType.Added)
		]);
	}

	[Fact]
	public void DocsetUnderSubfolder_DoesNotMatchSiblingPrefix()
	{
		Environment.SetEnvironmentVariable("MODIFIED_FILES", "docs/foo.md docs-extra/bar.md");

		var (gitRoot, docset) = CreateDirs("docs");
		var tracker = new IntegrationGitRepositoryTracker(gitRoot, docset);

		var changes = tracker.GetChangedFiles();

		changes.Should().ContainSingle()
			.Which.Should().BeEquivalentTo(new GitChange("docs/foo.md", GitChangeType.Modified));
	}

	[Fact]
	public void RenamedFiles_ExposeOldAndNewPaths()
	{
		Environment.SetEnvironmentVariable("RENAMED_FILES", "docs/old.md:docs/new.md");

		var (gitRoot, docset) = CreateDirs("docs");
		var tracker = new IntegrationGitRepositoryTracker(gitRoot, docset);

		var changes = tracker.GetChangedFiles();

		changes.Should().ContainSingle()
			.Which.Should().BeEquivalentTo(new RenamedGitChange("docs/old.md", "docs/new.md", GitChangeType.Renamed));
	}
}
