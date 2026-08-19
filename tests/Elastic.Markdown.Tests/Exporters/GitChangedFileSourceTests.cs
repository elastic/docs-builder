// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Markdown.Exporters.GitDiff;

namespace Elastic.Markdown.Tests.Exporters;

public class GitChangedFileSourceTests
{
	[Fact]
	public void ParseNameStatus_ParsesModifiedAndRenamedEntries()
	{
		var output = "M\u0000docs/guides/start.md\u0000R100\u0000docs/old.md\u0000docs/new.md\u0000";

		var changes = GitChangedFileSource.ParseNameStatus(output);

		changes.Should().HaveCount(2);
		changes[0].Path.Should().Be("docs/guides/start.md");
		changes[0].ChangeType.Should().Be(SourceFileChangeType.Modified);
		changes[1].Path.Should().Be("docs/old.md");
		changes[1].NewPath.Should().Be("docs/new.md");
		changes[1].ChangeType.Should().Be(SourceFileChangeType.Renamed);
	}

	[Fact]
	public void IntegrationChangedFileSource_ReadsCiEnvironmentVariables()
	{
		var env = new DictionaryEnvironmentVariables(new Dictionary<string, string?>
		{
			["MODIFIED_FILES"] = "docs/guides/start.md docs/other.md",
			["ADDED_FILES"] = "docs/new.md",
			["DELETED_FILES"] = "docs/removed.md",
			["RENAMED_FILES"] = "docs/old.md:docs/renamed.md"
		});

		var result = IntegrationChangedFileSource.GetChanges("docs", env, "origin/main");

		result.Base.Should().Be("origin/main");
		result.Changes.Should().HaveCount(4);
		result.Changes.Should().Contain(c => c.Path == "docs/guides/start.md" && c.ChangeType == SourceFileChangeType.Modified);
		result.Changes.Should().Contain(c => c.Path == "docs/new.md" && c.ChangeType == SourceFileChangeType.Added);
		result.Changes.Should().Contain(c => c.Path == "docs/removed.md" && c.ChangeType == SourceFileChangeType.Deleted);
		result.Changes.Should().Contain(c => c.Path == "docs/old.md" && c.NewPath == "docs/renamed.md");
	}

	private sealed class DictionaryEnvironmentVariables(Dictionary<string, string?> values) : IEnvironmentVariables
	{
		public string? GetEnvironmentVariable(string name) =>
			values.TryGetValue(name, out var value) ? value : null;

		public bool IsRunningOnCI => true;
	}
}
