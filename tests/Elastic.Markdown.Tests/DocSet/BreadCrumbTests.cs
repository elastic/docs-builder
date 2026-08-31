// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;

namespace Elastic.Markdown.Tests.DocSet;

public class BreadCrumbTests(ITestOutputHelper output) : NavigationTestsBase(output)
{
	[Fact]
	public void CanQueryParentsSuccessfully()
	{
		var documentationSet = Generator.DocumentationSet;
		INavigationTraversable navigationTraversable = documentationSet;
		var crossLinks = Generator.DocumentationSet.MarkdownFiles.ToDictionary(f => f.CrossLink);
		var allKeys = crossLinks.Keys.ToList();

		var lookup = Path.Join("nested", "index.md");
		var doc = Generator
			.DocumentationSet
			.MarkdownFiles
			.FirstOrDefault(f => f.SourceFile.FullName.EndsWith(lookup, StringComparison.OrdinalIgnoreCase));

		doc.Should().NotBeNull();

		var deeplyNestedDoc = Generator
			.DocumentationSet
			.MarkdownFiles
			.FirstOrDefault(f => f.RelativePath.OptionalWindowsReplace().EndsWith("deeply-nested/foo.md", StringComparison.Ordinal));
		deeplyNestedDoc.Should().NotBeNull();

		crossLinks.Should().ContainKey(doc.CrossLink);
		var nav = navigationTraversable.GetNavigationFor(crossLinks[doc.CrossLink]);

		nav.Parent.Should().NotBeNull();

		var docNavigation = navigationTraversable.GetNavigationFor(doc);
		docNavigation.Should().NotBeNull();
		var parents = navigationTraversable.GetParentsOfMarkdownFile(doc);

		parents.Should().HaveCount(1);
	}
}
