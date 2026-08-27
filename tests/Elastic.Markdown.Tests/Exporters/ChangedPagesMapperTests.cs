// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Exporters.GitDiff;

namespace Elastic.Markdown.Tests.Exporters;

public class ChangedPagesMapperTests
{
	private static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> EmptyIncludeIndex =
		new Dictionary<string, IReadOnlyCollection<string>>();

	private static readonly Dictionary<string, BuiltPageInfo> SamplePages = new(StringComparer.OrdinalIgnoreCase)
	{
		["guides/start.md"] = new("/preview/guides/start", "Get started"),
		["reference/index.md"] = new("/preview/reference", "Reference"),
		["reference/page.md"] = new("/preview/reference/page", "A page"),
	};

	[Fact]
	public void Map_DirectPageChange_ReturnsPageWithUrlAndTitle()
	{
		var changes = new[] { new SourceFileChange("docs/guides/start.md", SourceFileChangeType.Modified) };

		var export = ChangedPagesMapper.Map("origin/main", "docs", SamplePages, EmptyIncludeIndex, changes);

		export.Pages.Should().ContainSingle();
		export.Pages[0].SourcePath.Should().Be("guides/start.md");
		export.Pages[0].Url.Should().Be("/preview/guides/start");
		export.Pages[0].Title.Should().Be("Get started");
		export.Pages[0].Change.Should().Be("modified");
		export.Pages[0].IncludedFrom.Should().BeEmpty();
	}

	[Fact]
	public void Map_SnippetChange_ReturnsPagesThatIncludeIt()
	{
		var includeIndex = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
		{
			["_snippets/shared.md"] = ["guides/start.md", "reference/page.md"]
		};
		var changes = new[] { new SourceFileChange("docs/_snippets/shared.md", SourceFileChangeType.Modified) };

		var export = ChangedPagesMapper.Map("origin/main", "docs", SamplePages, includeIndex, changes);

		export.Pages.Should().HaveCount(2);
		export.Pages.Should().OnlyContain(p => p.Change == "modified");
		export.Pages.Should().AllSatisfy(p => p.IncludedFrom.Should().Equal(["_snippets/shared.md"]));
	}

	[Fact]
	public void Map_ConfigFileChange_SetsConfigChangedWithoutPages()
	{
		var changes = new[] { new SourceFileChange("docs/docset.yml", SourceFileChangeType.Modified) };

		var export = ChangedPagesMapper.Map("origin/main", "docs", SamplePages, EmptyIncludeIndex, changes);

		export.ConfigChanged.Should().BeTrue();
		export.Pages.Should().BeEmpty();
	}

	[Fact]
	public void Map_DeletedPage_AddsDeletedEntry()
	{
		var changes = new[] { new SourceFileChange("docs/reference/page.md", SourceFileChangeType.Deleted) };

		var export = ChangedPagesMapper.Map("origin/main", "docs", SamplePages, EmptyIncludeIndex, changes);

		export.Deleted.Should().ContainSingle(d => d.SourcePath == "reference/page.md");
		export.Pages.Should().BeEmpty();
	}

	[Fact]
	public void Map_RenamedPage_AddsDeletedOldPathAndNewPage()
	{
		var pages = new Dictionary<string, BuiltPageInfo>(SamplePages, StringComparer.OrdinalIgnoreCase)
		{
			["guides/new-start.md"] = new("/preview/guides/new-start", "Get started")
		};
		var changes = new[]
		{
			new SourceFileChange("docs/guides/start.md", SourceFileChangeType.Renamed, "docs/guides/new-start.md")
		};

		var export = ChangedPagesMapper.Map("origin/main", "docs", pages, EmptyIncludeIndex, changes);

		export.Deleted.Should().ContainSingle(d => d.SourcePath == "guides/start.md");
		export.Pages.Should().ContainSingle(p => p.SourcePath == "guides/new-start.md" && p.Change == "renamed");
	}

	[Fact]
	public void Map_IndexPage_UsesNavigationUrlWithoutIndexSuffix()
	{
		var changes = new[] { new SourceFileChange("docs/reference/index.md", SourceFileChangeType.Modified) };

		var export = ChangedPagesMapper.Map("origin/main", "docs", SamplePages, EmptyIncludeIndex, changes);

		export.Pages.Should().ContainSingle(p => p.Url == "/preview/reference");
	}

	[Fact]
	public void Map_AddedPage_UsesAddedChangeLabel()
	{
		var changes = new[] { new SourceFileChange("docs/guides/start.md", SourceFileChangeType.Added) };

		var export = ChangedPagesMapper.Map("origin/main", "docs", SamplePages, EmptyIncludeIndex, changes);

		export.Pages.Should().ContainSingle(p => p.SourcePath == "guides/start.md" && p.Change == "added");
	}

	[Fact]
	public void Map_PathOutsideDocset_IsIgnored()
	{
		var changes = new[] { new SourceFileChange("README.md", SourceFileChangeType.Modified) };

		var export = ChangedPagesMapper.Map("origin/main", "docs", SamplePages, EmptyIncludeIndex, changes);

		export.Pages.Should().BeEmpty();
		export.Deleted.Should().BeEmpty();
		export.ConfigChanged.Should().BeFalse();
	}
}
