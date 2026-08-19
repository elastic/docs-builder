// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Markdown.Exporters.GitDiff;
using Microsoft.Extensions.Logging.Abstractions;

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
	public void ParseNameStatus_ParsesAddedDeletedAndCopiedEntries()
	{
		var output = "A\u0000docs/new.md\u0000D\u0000docs/gone.md\u0000C100\u0000docs/src.md\u0000docs/copy.md\u0000";

		var changes = GitChangedFileSource.ParseNameStatus(output);

		changes.Should().HaveCount(3);
		changes[0].ChangeType.Should().Be(SourceFileChangeType.Added);
		changes[1].ChangeType.Should().Be(SourceFileChangeType.Deleted);
		changes[2].ChangeType.Should().Be(SourceFileChangeType.Renamed);
		changes[2].Path.Should().Be("docs/src.md");
		changes[2].NewPath.Should().Be("docs/copy.md");
	}

	[Fact]
	public void ParseNameStatus_IgnoresTruncatedRename()
	{
		var changes = GitChangedFileSource.ParseNameStatus("R100\u0000docs/old.md\u0000");

		changes.Should().BeEmpty();
	}

	[Fact]
	public void IntegrationChangedFileSource_ReadsCiEnvironmentVariables()
	{
		var env = new FakeEnvironmentVariables(new Dictionary<string, string?>
		{
			["MODIFIED_FILES"] = "docs/guides/start.md docs/other.md",
			["ADDED_FILES"] = "docs/new.md",
			["DELETED_FILES"] = "docs/removed.md",
			["RENAMED_FILES"] = "docs/old.md:docs/renamed.md"
		});

		var result = IntegrationChangedFileSource.GetChanges("docs", env, "origin/main");

		result.Base.Should().Be("origin/main");
		result.Changes.Should().HaveCount(5);
		result.Changes.Should().Contain(c => c.Path == "docs/guides/start.md" && c.ChangeType == SourceFileChangeType.Modified);
		result.Changes.Should().Contain(c => c.Path == "docs/new.md" && c.ChangeType == SourceFileChangeType.Added);
		result.Changes.Should().Contain(c => c.Path == "docs/removed.md" && c.ChangeType == SourceFileChangeType.Deleted);
		result.Changes.Should().Contain(c => c.Path == "docs/old.md" && c.NewPath == "docs/renamed.md");
	}

	[Fact]
	public void IntegrationChangedFileSource_DropsPathsOutsideDocset()
	{
		var env = new FakeEnvironmentVariables(new Dictionary<string, string?>
		{
			["MODIFIED_FILES"] = "docs/page.md README.md",
			["RENAMED_FILES"] = "src/old.md:src/new.md"
		});

		var result = IntegrationChangedFileSource.GetChanges("docs", env, "ci");

		result.Changes.Should().ContainSingle(c => c.Path == "docs/page.md");
	}

	[Fact]
	public void GetChanges_UsesDocsDiffBaseAndParsesGitStdout()
	{
		var calls = new List<string[]>();
		string Git(string[] args)
		{
			calls.Add(args);
			return "M\u0000docs/guides/start.md\u0000A\u0000docs/new.md\u0000";
		}

		var result = CreateSource(
			new FakeEnvironmentVariables(new Dictionary<string, string?> { ["DOCS_DIFF_BASE"] = "origin/main" }),
			Git
		).GetChanges();

		result.Base.Should().Be("origin/main");
		result.Changes.Should().HaveCount(2);
		calls.Should().ContainSingle();
		calls[0].Should().Equal("diff", "--name-status", "-z", "origin/main", "HEAD", "--", "./docs");
	}

	[Fact]
	public void GetChanges_PrefixesGithubBaseRef()
	{
		var result = CreateSource(
			new FakeEnvironmentVariables(new Dictionary<string, string?> { ["GITHUB_BASE_REF"] = "main" }),
			static args => args[0] == "diff" ? "M\u0000docs/page.md\u0000" : string.Empty
		).GetChanges();

		result.Base.Should().Be("origin/main");
		result.Changes.Should().ContainSingle(c => c.Path == "docs/page.md");
	}

	[Fact]
	public void GetChanges_SkipsGitWhenCiFileListIsSet()
	{
		var result = CreateSource(
			new FakeEnvironmentVariables(new Dictionary<string, string?>
			{
				["MODIFIED_FILES"] = "docs/page.md",
				["DOCS_DIFF_BASE"] = "origin/main"
			}),
			static _ => throw new InvalidOperationException("git should not run when a CI file list is set")
		).GetChanges();

		result.Base.Should().Be("origin/main");
		result.Changes.Should().ContainSingle(c => c.Path == "docs/page.md");
	}

	[Fact]
	public void GetChanges_EmptyGitOutputYieldsNoChanges()
	{
		var result = CreateSource(
			new FakeEnvironmentVariables(new Dictionary<string, string?> { ["DOCS_DIFF_BASE"] = "origin/main" }),
			static _ => string.Empty
		).GetChanges();

		result.Changes.Should().BeEmpty();
	}

	[Fact]
	public void GetChanges_OmitsPathspecWhenDocsetPrefixIsEmpty()
	{
		string[]? diffArgs = null;
		string Git(string[] args)
		{
			if (args[0] == "diff")
				diffArgs = args;
			return string.Empty;
		}

		_ = CreateSource(
			new FakeEnvironmentVariables(new Dictionary<string, string?> { ["DOCS_DIFF_BASE"] = "HEAD~1" }),
			Git,
			docsetPrefix: ""
		).GetChanges();

		diffArgs.Should().Equal("diff", "--name-status", "-z", "HEAD~1", "HEAD");
	}

	private static GitChangedFileSource CreateSource(
		FakeEnvironmentVariables environment,
		Func<string[], string> gitCommand,
		string docsetPrefix = "docs"
	)
	{
		var fileSystem = new MockFileSystem();
		return new GitChangedFileSource(
			NullLoggerFactory.Instance,
			fileSystem.DirectoryInfo.New("/repo"),
			docsetPrefix,
			environment,
			gitCommand
		);
	}
}
