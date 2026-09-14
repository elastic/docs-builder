// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;

namespace Elastic.Changelog.Tests.Utilities;

public class FilterLoaderUtilitiesTests
{
	private static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

	[Fact]
	public void ExpandTilde_BareTilde_ReturnsHomeDirectory()
	{
		var result = FilterLoaderUtilities.ExpandTilde("~");

		result.Should().Be(Home);
	}

	[Fact]
	public void ExpandTilde_TildePrefixedPath_ExpandsToHomeDirectory()
	{
		var result = FilterLoaderUtilities.ExpandTilde("~/docs/changelog/entry.yaml");

		result.Should().Be(Path.Join(Home, "docs/changelog/entry.yaml"));
	}

	[Fact]
	public void ExpandTilde_RelativePath_ReturnsTrimmedPathUnchanged()
	{
		var result = FilterLoaderUtilities.ExpandTilde(" docs/changelog/entry.yaml ");

		result.Should().Be("docs/changelog/entry.yaml");
	}

	[Fact]
	public void ExpandTilde_TildeInMiddleOfPath_ReturnsPathUnchanged()
	{
		var result = FilterLoaderUtilities.ExpandTilde("docs/~backup/entry.yaml");

		result.Should().Be("docs/~backup/entry.yaml");
	}
}
