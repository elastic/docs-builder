// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Tests.Inline;

namespace Elastic.Markdown.Tests.TaskList;

public class BasicTaskListTests(ITestOutputHelper output)
	: InlineTest(output, """
- [ ] A pending task
- [x] A completed task
""")
{
	[Fact]
	public void RendersTaskListContainer() =>
		Html.Should().Contain("class=\"contains-task-list\"");

	[Fact]
	public void RendersTaskListItem() =>
		Html.Should().Contain("class=\"task-list-item\"");

	[Fact]
	public void RendersUncheckedCheckbox() =>
		Html.ShouldContainHtml("""<input disabled="disabled" type="checkbox">""");

	[Fact]
	public void RendersCheckedCheckbox() =>
		Html.ShouldContainHtml("""<input disabled="disabled" type="checkbox" checked="checked">""");
}
