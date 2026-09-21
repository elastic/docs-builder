// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Markdown.Tests.DocSet;

public class NavigationTests(ITestOutputHelper output) : NavigationTestsBase(output)
{
	[Fact]
	public void ParsesATableOfContents() => Set.Navigation.Should().NotBeNull();

	[Fact]
	public void ParsesRedirects()
	{
		Configuration.Should().NotBeNull();

		Configuration
			.Redirects
			.Should()
			.NotBeNullOrEmpty()
			.And
			.ContainKey("redirects/first-page-old.md")
			.And
			.ContainKey("redirects/second-page-old.md")
			.And
			.ContainKey("redirects/4th-page.md")
			.And
			.ContainKey("redirects/third-page.md");

		var redirect1 = Configuration.Redirects["redirects/first-page-old.md"];
		redirect1.To.Should().Be("redirects/second-page.md");

		var redirect2 = Configuration.Redirects["redirects/second-page-old.md"];
		redirect2.Many.Should().NotBeNullOrEmpty().And.HaveCount(2);
		redirect2.Many[0].To.Should().Be("redirects/second-page.md");
		redirect2.Many[1].To.Should().Be("redirects/third-page.md");
		redirect2.To.Should().BeNullOrEmpty();

		var redirect3 = Configuration.Redirects["redirects/third-page.md"];
		redirect3.To.Should().Be("redirects/third-page.md");

		var redirect4 = Configuration.Redirects["redirects/4th-page.md"];
		redirect4.To.Should().Be("redirects/5th-page.md");
	}
}
