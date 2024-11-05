using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.IO;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Markdown.Tests.SiteMap;

public class NavigationTests
{
	[Fact]
	public async Task CreatesDefaultOutputDirectory()
	{
		var logger = NullLoggerFactory.Instance;
		var readFs = new FileSystem(); //use real IO to read docs.
		var writeFs = new MockFileSystem(new MockFileSystemOptions //use in memory mock fs to test generation
		{
			CurrentDirectory = Paths.Root.FullName
		});

		var set = new DocumentationSet(readFs);

		set.Files.Should().HaveCountGreaterThan(10);
		var generator = new DocumentationGenerator(set, logger, readFs, writeFs);

		await generator.GenerateAll(default);

		writeFs.Directory.Exists(".artifacts/docs/html").Should().BeTrue();
		readFs.Directory.Exists(".artifacts/docs/html").Should().BeFalse();

	}
}