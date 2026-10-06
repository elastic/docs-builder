// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using CsCheck;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation.DocumentationSets;

namespace Elastic.ApiExplorer.Tests;

// Property tests document their law and its consequence on purpose: a generated input is invisible and a law is not a
// value, so the doc comment is the only place a reader learns what is guaranteed and why it matters.

/// <summary>Named generators for the API child slug property tests.</summary>
internal static class SlugGens
{
	/// <summary>
	/// A child page file name: a stem of letters, digits, spaces, '_', '-' and '.', an optional version suffix such as
	/// '.v2', '.V10' or a malformed '.vx', and the '.md' extension.
	/// </summary>
	public static Gen<string> ChildFileName { get; } = Gen.Select(
		Gen.String[Gen.Char["a-zA-Z0-9 _.-"], 1, 12],
		Gen.OneOfConst("", ".v2", ".V10", ".v0", ".vx", ".v"),
		(stem, version) => $"{stem}{version}.md"
	);
}

public class ApiChildSlugProperties
{
	/// <summary>
	/// Law: the domain derives the same slug for a child page as API Explorer does when it builds the page's URL.
	/// Consequence if broken: the reserved-slug warning checks a different slug than the one published, so a child page
	/// can take a reserved API Explorer path without a warning, or be warned about a path it doesn't use.
	/// </summary>
	[Test]
	public void DomainSlug_MatchesApiExplorerSlug() =>
		SlugGens.ChildFileName.Sample(fileName =>
		{
			var file = new MockFileSystem().FileInfo.New($"/docs/api/elasticsearch/{fileName}");
			DocumentationSetConstraints.ApiChildSlug(fileName).Should().Be(SimpleMarkdownNavigationItem.CreateSlugFromFile(file));
		});
}
