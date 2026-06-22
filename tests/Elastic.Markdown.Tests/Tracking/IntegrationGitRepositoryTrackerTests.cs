// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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

	[Fact]
	public void DocsetUnderSubfolder_FiltersPathsByPrefix()
	{
		Environment.SetEnvironmentVariable("DELETED_FILES", "docs/foo.md docs-extra/bar.md other/baz.md");

		var tracker = new IntegrationGitRepositoryTracker("docs");

		var changes = tracker.GetChangedFiles();

		changes.Should().ContainSingle()
			.Which.Should().BeEquivalentTo(new GitChange("docs/foo.md", GitChangeType.Deleted));
	}

	[Fact]
	public void DocsetAtRepoRoot_DotLookupPath_ReturnsAllChanges()
	{
		// Regression test for https://github.com/elastic/docs-content/pull/6479:
		// when the docset lives at the repo root (e.g. docs-content), the relative
		// path resolves to ".", which previously turned the prefix filter into "./"
		// and silently dropped every file from the env vars.
		Environment.SetEnvironmentVariable("DELETED_FILES", "troubleshoot/deployments/serverless.md");
		Environment.SetEnvironmentVariable("MODIFIED_FILES", "troubleshoot/toc.yml");

		var tracker = new IntegrationGitRepositoryTracker(".");

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

		var tracker = new IntegrationGitRepositoryTracker("");

		var changes = tracker.GetChangedFiles();

		changes.Should().BeEquivalentTo(
		[
			new GitChange("a.md", GitChangeType.Added),
			new GitChange("b.md", GitChangeType.Added)
		]);
	}

	[Fact]
	public void LookupPath_NormalizesSurroundingSlashes()
	{
		Environment.SetEnvironmentVariable("MODIFIED_FILES", "docs/foo.md docs-extra/bar.md");

		var tracker = new IntegrationGitRepositoryTracker("/docs/");

		var changes = tracker.GetChangedFiles();

		changes.Should().ContainSingle()
			.Which.Should().BeEquivalentTo(new GitChange("docs/foo.md", GitChangeType.Modified));
	}

	[Fact]
	public void RenamedFiles_ExposeOldAndNewPaths()
	{
		Environment.SetEnvironmentVariable("RENAMED_FILES", "docs/old.md:docs/new.md");

		var tracker = new IntegrationGitRepositoryTracker("docs");

		var changes = tracker.GetChangedFiles();

		changes.Should().ContainSingle()
			.Which.Should().BeEquivalentTo(new RenamedGitChange("docs/old.md", "docs/new.md", GitChangeType.Renamed));
	}
}
