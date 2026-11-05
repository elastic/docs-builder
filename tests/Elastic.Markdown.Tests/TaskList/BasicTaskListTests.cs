// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Tests.Inline;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.TaskList;

public class BasicTaskListTests() : InlineTest("""
- [ ] A pending task
- [x] A completed task
""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<ul class="contains-task-list">
			<li class="task-list-item">
				<input disabled="disabled" type="checkbox">
				A pending task</li>
			<li class="task-list-item">
				<input disabled="disabled" type="checkbox" checked="checked">
				A completed task</li>
		</ul>
		""",
			DocsHtml.Body()
		);

	[Test]
	public void RendersUncheckedCheckbox() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
			<ul class="contains-task-list">
				<li class="task-list-item">
					<input disabled="disabled" type="checkbox">
					A pending task</li>
				<li class="task-list-item">
					<input disabled="disabled" type="checkbox" checked="checked">
					A completed task</li>
			</ul>
			""",
			DocsHtml.Body()
		);

	[Test]
	public void RendersCheckedCheckbox() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
			<ul class="contains-task-list">
				<li class="task-list-item">
					<input disabled="disabled" type="checkbox">
					A pending task</li>
				<li class="task-list-item">
					<input disabled="disabled" type="checkbox" checked="checked">
					A completed task</li>
			</ul>
			""",
			DocsHtml.Body()
		);
}
