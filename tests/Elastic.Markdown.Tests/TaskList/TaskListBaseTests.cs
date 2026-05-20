using Elastic.Markdown.Tests.Inline;

namespace Elastic.Markdown.Tests.TaskLists;

public abstract class TaskListTest(ITestOutputHelper output, string content)
    : InlineTest(output, content);
