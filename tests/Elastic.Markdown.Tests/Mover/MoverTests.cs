// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Refactor;
using Elastic.Markdown.Tests.DocSet;
using FluentAssertions;


namespace Elastic.Markdown.Tests.Mover;


public class MoverTests(ITestOutputHelper output) : NavigationTestsBase(output)
{
	[Fact]
	public async Task RelativeLinks()
	{
		var workingDirectory = Set.Configuration.SourceFile.DirectoryName;
		Directory.SetCurrentDirectory(workingDirectory!);

		var mover = new Move(ReadFileSystem, WriteFileSystem, Set, LoggerFactory);
		await mover.Execute("testing/mover/first-page.md", "new-folder/hello-world.md", true, TestContext.Current.CancellationToken);

		mover.Changes.Should().HaveCount(1);
		var changeSet = mover.Changes.First();

		var linkModifications = mover.LinkModifications[changeSet];
		linkModifications.Should().HaveCount(3);


		Path.GetRelativePath(".", linkModifications[0].SourceFile).Should().Be(Path.Combine("testing", "mover", "first-page.md"));
		linkModifications[0].OldLink.Should().Be("[Link to second page](second-page.md)");
		linkModifications[0].NewLink.Should().Be("[Link to second page](../testing/mover/second-page.md)");

		Path.GetRelativePath(".", linkModifications[1].SourceFile).Should().Be(Path.Combine("testing", "mover", "second-page.md"));
		linkModifications[1].OldLink.Should().Be("[Link to first page](first-page.md)");
		linkModifications[1].NewLink.Should().Be("[Link to first page](../../new-folder/hello-world.md)");

		Path.GetRelativePath(".", linkModifications[2].SourceFile).Should().Be(Path.Combine("testing", "mover", "second-page.md"));
		linkModifications[2].OldLink.Should().Be("[Absolut link to first page](/testing/mover/first-page.md)");
		linkModifications[2].NewLink.Should().Be("[Absolut link to first page](/new-folder/hello-world.md)");
	}

	[Fact]
	public async Task MoveToFolder()
	{
		var workingDirectory = Set.Configuration.SourceFile.DirectoryName;
		Directory.SetCurrentDirectory(workingDirectory!);

		var mover = new Move(ReadFileSystem, WriteFileSystem, Set, LoggerFactory);
		await mover.Execute("testing/mover/first-page.md", "new-folder", true, TestContext.Current.CancellationToken);

		mover.Changes.Should().HaveCount(1);
		var changeSet = mover.Changes.First();

		var linkModifications = mover.LinkModifications[changeSet];
		linkModifications.Should().HaveCount(3);

		Path.GetRelativePath(".", linkModifications[0].SourceFile).Should().Be(Path.Combine("testing", "mover", "first-page.md"));
		linkModifications[0].OldLink.Should().Be("[Link to second page](second-page.md)");
		linkModifications[0].NewLink.Should().Be("[Link to second page](../testing/mover/second-page.md)");

		Path.GetRelativePath(".", linkModifications[1].SourceFile).Should().Be(Path.Combine("testing", "mover", "second-page.md"));
		linkModifications[1].OldLink.Should().Be("[Link to first page](first-page.md)");
		linkModifications[1].NewLink.Should().Be("[Link to first page](../../new-folder/first-page.md)");

		Path.GetRelativePath(".", linkModifications[2].SourceFile).Should().Be(Path.Combine("testing", "mover", "second-page.md"));
		linkModifications[2].OldLink.Should().Be("[Absolut link to first page](/testing/mover/first-page.md)");
		linkModifications[2].NewLink.Should().Be("[Absolut link to first page](/new-folder/first-page.md)");
	}

	[Fact]
	public async Task MoveFolderToFolder()
	{
		var workingDirectory = Set.Configuration.SourceFile.DirectoryName;
		Directory.SetCurrentDirectory(workingDirectory!);

		var mover = new Move(ReadFileSystem, WriteFileSystem, Set, LoggerFactory);
		await mover.Execute("testing/mover", "new-folder", true, TestContext.Current.CancellationToken);

		mover.Changes.Should().HaveCount(2);
		var changeSet = mover.LinkModifications.FirstOrDefault(k => k.Key.From.Name == "first-page.md").Key;

		var linkModifications = mover.LinkModifications[changeSet];
		linkModifications.Should().HaveCount(3);

		Path.GetRelativePath(".", linkModifications[0].SourceFile).Should().Be(Path.Combine("testing", "mover", "first-page.md"));
		linkModifications[0].OldLink.Should().Be("[Link to second page](second-page.md)");
		linkModifications[0].NewLink.Should().Be("[Link to second page](../testing/mover/second-page.md)");

		Path.GetRelativePath(".", linkModifications[1].SourceFile).Should().Be(Path.Combine("testing", "mover", "second-page.md"));
		linkModifications[1].OldLink.Should().Be("[Link to first page](first-page.md)");
		linkModifications[1].NewLink.Should().Be("[Link to first page](../../new-folder/first-page.md)");

		Path.GetRelativePath(".", linkModifications[2].SourceFile).Should().Be(Path.Combine("testing", "mover", "second-page.md"));
		linkModifications[2].OldLink.Should().Be("[Absolut link to first page](/testing/mover/first-page.md)");
		linkModifications[2].NewLink.Should().Be("[Absolut link to first page](/new-folder/first-page.md)");
	}
}
