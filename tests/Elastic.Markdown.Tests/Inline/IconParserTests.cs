// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Inline;

public class IconParserTests() : InlineTest(
	"""
	A check mark {icon}`check`. A cross {icon}`cross`. A warning {icon}`warning`.

	An unknown icon {icon}`not_a_real_icon` should not be replaced.
	This should not be an icon either/this:apm_trace:is:not:an:icon.
	Nor should this be an icon :invalid-icon:.
	An empty one is not an icon either ::.
	"""
)
{
	[Test]
	public void Render() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>A check mark
			<span aria-label="Icon for check" class="icon icon-check">
				<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
					<path fill-rule="evenodd" d="M15.354 4.354 6.5 13.207 1.646 8.354l.708-.708L6.5 11.793l8.146-8.147.708.708Z" clip-rule="evenodd"></path>
				</svg>
			</span>. A cross
			<span aria-label="Icon for cross" class="icon icon-cross">
				<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
					<path fill-rule="evenodd" d="M7.293 8 2.646 3.354l.708-.708L8 7.293l4.646-4.647.708.708L8.707 8l4.647 4.646-.707.708L8 8.707l-4.646 4.647-.708-.707L7.293 8Z" clip-rule="evenodd"></path>
				</svg>
			</span>. A warning
			<span aria-label="Icon for warning" class="icon icon-warning">
				<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
					<path d="M9 12a1 1 0 1 1-2 0 1 1 0 0 1 2 0Z"></path>
					<path fill-rule="evenodd" d="M7.5 10V5h1v5h-1Z" clip-rule="evenodd"></path>
					<path fill-rule="evenodd" d="M8 1a1 1 0 0 1 .864.496l7 12A1 1 0 0 1 15 15H1a1 1 0 0 1-.864-1.504l7-12A1 1 0 0 1 8 1ZM1 14h14L8 2 1 14Z" clip-rule="evenodd"></path>
				</svg>
			</span>.</p>
		<p>An unknown icon
			<span aria-label="Icon for " class="icon icon-"></span>
			should not be replaced. This should not be an icon either/this:apm_trace:is:not:an:icon. Nor should this be an icon :invalid-icon:. An empty one is not an icon either ::.</p>
		""",
			DocsHtml.Body()
		);
}

public class IconInListItemTest() : InlineTest("""
	- {icon}`check` A check mark.
	""")
{
	[Test]
	public void Render() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<ul>
			<li>
				<span aria-label="Icon for check" class="icon icon-check">
					<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
						<path fill-rule="evenodd" d="M15.354 4.354 6.5 13.207 1.646 8.354l.708-.708L6.5 11.793l8.146-8.147.708.708Z" clip-rule="evenodd"></path>
					</svg>
				</span>
				A check mark.</li>
		</ul>
		""",
			DocsHtml.Body()
		);
}

public class IconInHeadingShouldBeRemovedFromAnchor() : InlineTest("""
	## Users {icon}`check`
	""")
{
	[Test]
	public void Render() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<div class="heading-wrapper" id="users">
			<h2>
				<a class="headerlink" href="#users">Users
					<span aria-label="Icon for check" class="icon icon-check">
						<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
							<path fill-rule="evenodd" d="M15.354 4.354 6.5 13.207 1.646 8.354l.708-.708L6.5 11.793l8.146-8.147.708.708Z" clip-rule="evenodd"></path>
						</svg>
					</span>
				</a>
			</h2>
		</div>
		""",
			DocsHtml.Body()
		);
}
