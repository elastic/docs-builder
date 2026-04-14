// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Codex.Sourcing;
using Elastic.Documentation.Configuration;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Navigation.Tests.Codex;

public class FindDocsetFileTests
{
	private static readonly string RepoRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, "repo");

	private static ScopedFileSystem CreateScopedFs(MockFileSystem mockFs) =>
		FileSystemFactory.ScopeCurrentWorkingDirectory(mockFs);

	[Fact]
	public void StandardPath_Found()
	{
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ Path.Join(RepoRoot, "docs/docset.yml"), new MockFileData("project: test") }
		});

		var result = CodexCloneService.FindDocsetFile(mockFs, mockFs.DirectoryInfo.New(RepoRoot));

		result.Should().NotBeNull();
		result.Name.Should().Be("docset.yml");
	}

	[Fact]
	public void NonStandardPath_FoundViaRecursion()
	{
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ Path.Join(RepoRoot, "docs-codex/docset.yml"), new MockFileData("project: test") }
		});

		var result = CodexCloneService.FindDocsetFile(mockFs, mockFs.DirectoryInfo.New(RepoRoot));

		result.Should().NotBeNull();
		result.Name.Should().Be("docset.yml");
	}

	[Fact]
	public void HiddenDirectory_SkippedByScopedFileSystem()
	{
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ Path.Join(RepoRoot, ".github/workflows/ci.yml"), new MockFileData("name: CI") },
			{ Path.Join(RepoRoot, "docs-codex/docset.yml"), new MockFileData("project: test") }
		});
		var scopedFs = CreateScopedFs(mockFs);

		var result = CodexCloneService.FindDocsetFile(scopedFs, scopedFs.DirectoryInfo.New(RepoRoot));

		result.Should().NotBeNull();
		result.Name.Should().Be("docset.yml");
	}

	[Fact]
	public void NoDocset_ReturnsNull()
	{
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ Path.Join(RepoRoot, "src/main.py"), new MockFileData("print('hello')") }
		});

		var result = CodexCloneService.FindDocsetFile(mockFs, mockFs.DirectoryInfo.New(RepoRoot));

		result.Should().BeNull();
	}

	[Fact]
	public void NodeModules_Skipped()
	{
		var mockFs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ Path.Join(RepoRoot, "node_modules/some-pkg/docset.yml"), new MockFileData("project: fake") }
		});

		var result = CodexCloneService.FindDocsetFile(mockFs, mockFs.DirectoryInfo.New(RepoRoot));

		result.Should().BeNull();
	}
}
