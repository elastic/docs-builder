// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Creation;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests.Changelogs.Create;

public class NoteCreationTests(ITestOutputHelper output) : CreateChangelogTestBase(output)
{
	[Fact]
	public async Task CreateNote_WithAllRequiredFields_WritesNoteFile()
	{
		var service = CreateService();
		var outputDir = CreateOutputDirectory();

		var input = new CreateChangelogArguments
		{
			Title = "Slow rollover fix",
			Type = "bug-fix",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Output = outputDir,
			IsNote = true
		};

		var result = await service.CreateNote(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var files = FileSystem.Directory.GetFiles(outputDir, "*.yml");
		files.Should().HaveCount(1);
		FileSystem.Path.GetFileName(files[0]).Should().StartWith("note-");
		var content = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		content.Should().Contain("Slow rollover fix");
		content.Should().Contain("bug-fix");
	}

	[Fact]
	public async Task CreateNote_ProductWithoutTarget_ReturnsError()
	{
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Known issue",
			Type = "known-issue",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "*", Lifecycle = "ga" }],
			Output = CreateOutputDirectory(),
			IsNote = true
		};

		var result = await service.CreateNote(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("elasticsearch") && d.Message.Contains("target"));
	}

	[Fact]
	public async Task CreateNote_EmptyTarget_ReturnsError()
	{
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Known issue",
			Type = "known-issue",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "", Lifecycle = "ga" }],
			Output = CreateOutputDirectory(),
			IsNote = true
		};

		var result = await service.CreateNote(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("elasticsearch") && d.Message.Contains("target"));
	}

	[Fact]
	public async Task CreateNote_NameOverridesSlug_UsesProvidedName()
	{
		var service = CreateService();
		var outputDir = CreateOutputDirectory();

		var input = new CreateChangelogArguments
		{
			Title = "Some very long title that would produce a different slug",
			Type = "known-issue",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Output = outputDir,
			IsNote = true,
			NoteName = "tsdb-gap"
		};

		var result = await service.CreateNote(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yml");
		files.Should().HaveCount(1);
		FileSystem.Path.GetFileName(files[0]).Should().Be("note-tsdb-gap.yml");
	}

	[Fact]
	public async Task CreateNote_TitleSlugIsFilename_WhenNameAbsent()
	{
		var service = CreateService();
		var outputDir = CreateOutputDirectory();

		var input = new CreateChangelogArguments
		{
			Title = "Fix slow rollover",
			Type = "bug-fix",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Output = outputDir,
			IsNote = true
		};

		var result = await service.CreateNote(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yml");
		files.Should().HaveCount(1);
		FileSystem.Path.GetFileName(files[0]).Should().Be("note-fix-slow-rollover.yml");
	}

	[Fact]
	public async Task CreateNote_WithPrs_AllowedWithoutError()
	{
		var service = CreateService();
		var outputDir = CreateOutputDirectory();

		var input = new CreateChangelogArguments
		{
			Title = "Known limitation",
			Type = "known-issue",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.3.0", Lifecycle = "ga" }],
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Output = outputDir,
			IsNote = true,
			NoteName = "known-limitation"
		};

		var result = await service.CreateNote(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yml");
		files.Should().HaveCount(1);
		FileSystem.Path.GetFileName(files[0]).Should().Be("note-known-limitation.yml");
		var content = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		content.Should().Contain("pull/12345");
	}
}
