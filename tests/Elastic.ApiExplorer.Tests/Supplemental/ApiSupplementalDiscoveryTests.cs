// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Supplemental;

namespace Elastic.ApiExplorer.Tests.Supplemental;

public class ApiSupplementalDiscoveryTests
{
	private const string Folder = "/docs/api/fixture";

	[Fact]
	public void Discover_MissingFolder_ReturnsEmpty()
	{
		var fs = new MockFileSystem();
		var folder = fs.DirectoryInfo.New("/docs/api/missing");

		var result = ApiSupplementalDiscovery.Discover(folder, ["search"], ["search"]);

		result.Operations.Should().BeEmpty();
		result.Tags.Should().BeEmpty();
		result.Unmatched.Should().BeEmpty();
		result.Ignored.Should().BeEmpty();
		result.VersionSuffixed.Should().BeEmpty();
	}

	[Fact]
	public void Discover_NullFolder_ReturnsEmpty()
	{
		var result = ApiSupplementalDiscovery.Discover(null, ["search"], ["search"]);

		result.Operations.Should().BeEmpty();
	}

	[Fact]
	public void Discover_MatchesExactOperationId()
	{
		var folder = FolderWith(
			"op-search.md",
			"op-getAlertingHealth.md",
			"op-getalertinghealth.md",
			"op-cluster-health.md");

		var result = ApiSupplementalDiscovery.Discover(
			folder,
			["search", "getAlertingHealth", "cluster.health"],
			[]);

		result.Operations.Keys.Should().BeEquivalentTo("search", "getAlertingHealth");
		result.Unmatched.Select(f => f.Name).Should().BeEquivalentTo("op-getalertinghealth.md", "op-cluster-health.md");
	}

	[Fact]
	public void Discover_MatchesTagUrlSlug()
	{
		var folder = FolderWith(
			"tag-ml-anomaly.md",
			"tag-health_report.md",
			"tag-apm-agent-configuration.md");

		var result = ApiSupplementalDiscovery.Discover(
			folder,
			[],
			["ml anomaly", "health_report", "APM agent configuration"]);

		result.Tags.Should().ContainKey("ml anomaly");
		result.Tags.Should().ContainKey("health_report");
		result.Tags.Should().ContainKey("APM agent configuration");
		result.Unmatched.Should().BeEmpty();
	}

	[Fact]
	public void Discover_IgnoresNonConventionFiles()
	{
		var folder = FolderWith("random-notes.md", "getting-started.md", "op-search.md");

		var result = ApiSupplementalDiscovery.Discover(folder, ["search"], []);

		result.Ignored.Select(f => f.Name).Should().BeEquivalentTo("random-notes.md", "getting-started.md");
		result.Operations.Should().ContainKey("search");
	}

	[Fact]
	public void Discover_UnmatchedConventionFile_IsNotAnError()
	{
		var folder = FolderWith("op-does-not-exist.md");

		var result = ApiSupplementalDiscovery.Discover(folder, ["search"], []);

		result.Unmatched.Should().ContainSingle(f => f.Name == "op-does-not-exist.md");
		result.Operations.Should().BeEmpty();
	}

	[Fact]
	public void Discover_VersionSuffixedFile_IsClassifiedSeparately()
	{
		var folder = FolderWith("op-search.v8.md", "op-search.md");

		var result = ApiSupplementalDiscovery.Discover(folder, ["search"], []);

		result.Operations.Should().ContainKey("search");
		result.VersionSuffixed.Should().ContainSingle(v => v.File.Name == "op-search.v8.md" && v.Name.VersionMajor == 8);
		result.Unmatched.Should().BeEmpty();
	}

	[Fact]
	public void Discover_TagSlugCollision_IsRecordedAndFileUnmatched()
	{
		var folder = FolderWith("tag-foo-bar.md");

		var result = ApiSupplementalDiscovery.Discover(folder, [], ["foo bar", "foo-bar"]);

		result.TagSlugCollisions.Should().ContainSingle(c => c.Slug == "foo-bar");
		result.TagSlugCollisions[0].TagNames.Should().BeEquivalentTo("foo bar", "foo-bar");
		result.Tags.Should().BeEmpty();
		result.Unmatched.Should().ContainSingle(f => f.Name == "tag-foo-bar.md");
	}

	[Fact]
	public async Task Discover_FixtureDocument_MatchesSearchAndDocsGet()
	{
		var folder = FolderWith("op-search.md", "op-docs-get.md", "tag-search.md", "random-notes.md");
		var path = Path.Combine(AppContext.BaseDirectory, "TestData", "api-explorer-fixture.json");
		var document = await OpenApiReader.Instance.ReadAsync(new System.IO.Abstractions.FileSystem().FileInfo.New(path))
			?? throw new InvalidOperationException("Could not read fixture spec");

		var result = ApiSupplementalDiscovery.Discover(folder, document);

		result.Operations.Should().ContainKey("search");
		result.Operations.Should().ContainKey("docs-get");
		result.Tags.Should().ContainKey("search");
		result.Ignored.Should().ContainSingle(f => f.Name == "random-notes.md");
	}

	private static System.IO.Abstractions.IDirectoryInfo FolderWith(params string[] fileNames)
	{
		var files = fileNames.ToDictionary(
			name => $"{Folder}/{name}",
			_ => new MockFileData("# supplemental"));
		var fs = new MockFileSystem(files);
		return fs.DirectoryInfo.New(Folder);
	}
}
