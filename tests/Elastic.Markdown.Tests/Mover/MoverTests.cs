// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Documentation.Mover;
using Elastic.Markdown.Tests.DocSet;
using FluentAssertions;

using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Mover;


public class MoverTests(ITestOutputHelper output) : NavigationTestsBase(output)
{
	[Fact]
	public async Task RelativeLinks()
	{
		var workingDirectory = Set.Configuration.SourceFile.DirectoryName;
		Directory.SetCurrentDirectory(workingDirectory!);

		var mover = new Move(ReadFileSystem, WriteFileSystem, Set, LoggerFactory);
		await mover.Execute("testing/mover/first-page.md", "new-folder/hello-world.md", true);
		mover.LinkModifications.Should().HaveCount(3);

		Path.GetRelativePath(".", mover.LinkModifications[0].SourceFile).Should().Be("testing/mover/first-page.md");
		mover.LinkModifications[0].OldLink.Should().Be("[Link to second page](second-page.md)");
		mover.LinkModifications[0].NewLink.Should().Be("[Link to second page](../testing/mover/second-page.md)");

		Path.GetRelativePath(".", mover.LinkModifications[1].SourceFile).Should().Be("testing/mover/second-page.md");
		mover.LinkModifications[1].OldLink.Should().Be("[Link to first page](first-page.md)");
		mover.LinkModifications[1].NewLink.Should().Be("[Link to first page](../../new-folder/hello-world.md)");

		Path.GetRelativePath(".", mover.LinkModifications[2].SourceFile).Should().Be("testing/mover/second-page.md");
		mover.LinkModifications[2].OldLink.Should().Be("[Absolut link to first page](/testing/mover/first-page.md)");
		mover.LinkModifications[2].NewLink.Should().Be("[Absolut link to first page](/new-folder/hello-world.md)");
	}
}
