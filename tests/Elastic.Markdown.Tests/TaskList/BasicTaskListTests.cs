namespace Elastic.Markdown.Tests.TaskLists;

public class UncheckedTaskItemTests(ITestOutputHelper output)
    : TaskListTest(output, """
- [ ] A pending task
""")
{
    [Fact]
    public void RendersTaskListContainer() =>
        Html.Should().Contain("""class="contains-task-list"""");

    [Fact]
    public void RendersTaskListItem() =>
        Html.ShouldContainHtml("""<li class="task-list-item">""");

    [Fact]
    public void RendersUncheckedCheckbox() =>
        Html.ShouldContainHtml("""<input disabled="disabled" type="checkbox">""");
}
