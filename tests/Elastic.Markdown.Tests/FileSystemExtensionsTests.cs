// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;
using Elastic.Markdown.IO;
using FluentAssertions;

namespace Elastic.Markdown.Tests;

public class FileSystemExtensionsTest(ITestOutputHelper output)
{
	[Fact]
	public void IsSubPathOfTests()
	{
		var fs = new MockFileSystem();

		fs.DirectoryInfo.New("/a/b/c/d").IsSubPathOf(fs.DirectoryInfo.New("/a/b/c/d/e/f")).Should().BeFalse();
		fs.DirectoryInfo.New("/a/b/c/d/e/f").IsSubPathOf(fs.DirectoryInfo.New("/a/b/c/d")).Should().BeTrue();

		var caseSensitive = IDirectoryInfoExtensions.IsCaseSensitiveFileSystem;

		if (caseSensitive)
		{
			fs.DirectoryInfo.New("/a/b/C/d/e/f").IsSubPathOf(fs.DirectoryInfo.New("/a/b/c/d")).Should().BeFalse();
			fs.DirectoryInfo.New("/a/b/c/d/e/f").IsSubPathOf(fs.DirectoryInfo.New("/a/b/C/d")).Should().BeFalse();
		}
		else
		{
			fs.DirectoryInfo.New("/a/b/C/d/e/f").IsSubPathOf(fs.DirectoryInfo.New("/a/b/c/d")).Should().BeTrue();
			fs.DirectoryInfo.New("/a/b/c/d/e/f").IsSubPathOf(fs.DirectoryInfo.New("/a/b/C/d")).Should().BeTrue();
		}
	}

	[Fact]
	public void HasParentTests()
	{
		var fs = new MockFileSystem();

		fs.DirectoryInfo.New("/a/b/c/d").HasParent("c").Should().BeTrue();
		fs.DirectoryInfo.New("/a/b/c/d/e").HasParent("e").Should().BeTrue();

		fs.DirectoryInfo.New("/a/b/C/d").HasParent("c", StringComparison.Ordinal).Should().BeFalse();
		var caseSensitive = IDirectoryInfoExtensions.IsCaseSensitiveFileSystem;

		// HasParent is always case-insensitive by default
		if (caseSensitive)
			fs.DirectoryInfo.New("/a/b/C/d").HasParent("c").Should().BeTrue();
		else
			fs.DirectoryInfo.New("/a/b/C/d").HasParent("c").Should().BeTrue();
	}
}
