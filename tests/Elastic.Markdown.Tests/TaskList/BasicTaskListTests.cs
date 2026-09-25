// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Tests.Inline;

namespace Elastic.Markdown.Tests.TaskList;

public class BasicTaskListTests() : InlineTest("""
- [ ] A pending task
- [x] A completed task
""")
{
	[Test]
	public void RendersTaskListContainer() => Html.Should().Contain("class=\"contains-task-list\"");

	[Test]
	public void RendersTaskListItem() => Html.Should().Contain("class=\"task-list-item\"");

	[Test]
	public void RendersUncheckedCheckbox() => Html.ShouldContainHtml("""<input disabled="disabled" type="checkbox">""");

	[Test]
	public void RendersCheckedCheckbox() => Html.ShouldContainHtml("""<input disabled="disabled" type="checkbox" checked="checked">""");
}
