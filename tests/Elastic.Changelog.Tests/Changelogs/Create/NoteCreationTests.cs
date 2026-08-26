// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Creation;
using Elastic.Documentation;
using Elastic.Documentation.Diagnostics;
using FakeItEasy;

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
	public async Task CreateNote_NumericPrWithoutOwnerRepo_ReturnsError()
	{
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Known limitation",
			Type = "known-issue",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.3.0", Lifecycle = "ga" }],
			Prs = ["12345"],
			Output = CreateOutputDirectory(),
			IsNote = true
		};

		var result = await service.CreateNote(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("--owner") && d.Message.Contains("--repo"));
	}

	[Fact]
	public async Task CreateNote_NumericIssueWithoutOwnerRepo_ReturnsError()
	{
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Known limitation",
			Type = "known-issue",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.3.0", Lifecycle = "ga" }],
			Issues = ["456"],
			Output = CreateOutputDirectory(),
			IsNote = true
		};

		var result = await service.CreateNote(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("--owner") && d.Message.Contains("--repo"));
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

	[Fact]
	public async Task CreateNote_MixedNumericAndUrlPrWithoutOwnerRepo_ReturnsError()
	{
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Known limitation",
			Type = "known-issue",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.3.0", Lifecycle = "ga" }],
			Prs = ["12345", "https://github.com/elastic/elasticsearch/pull/999"],
			Output = CreateOutputDirectory(),
			IsNote = true
		};

		var result = await service.CreateNote(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("--owner") && d.Message.Contains("--repo"));
	}

	[Fact]
	public async Task CreateNote_MixedNumericAndUrlIssueWithoutOwnerRepo_ReturnsError()
	{
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Known limitation",
			Type = "known-issue",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.3.0", Lifecycle = "ga" }],
			Issues = ["456", "https://github.com/elastic/elasticsearch/issues/789"],
			Output = CreateOutputDirectory(),
			IsNote = true
		};

		var result = await service.CreateNote(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("--owner") && d.Message.Contains("--repo"));
	}

	[Fact]
	public async Task CreateNote_InCI_ExtractionDisabledByCli_ClearsCIDescription()
	{
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature: "type:feature"
			    bug-fix: "type:bug-fix"
			    breaking-change: "type:breaking-change"
			    known-issue:
			lifecycles:
			  - preview
			  - beta
			  - ga
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var env = A.Fake<IEnvironmentVariables>();
		A.CallTo(() => env.IsRunningOnCI).Returns(true);
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_PR_NUMBER")).Returns(null);
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_TITLE")).Returns(null);
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_DESCRIPTION")).Returns("CI injected description that should be suppressed");
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_TYPE")).Returns(null);
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_OWNER")).Returns(null);
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_REPO")).Returns(null);
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_PRODUCTS")).Returns(null);

		var service = CreateService(env);
		var outputDir = CreateOutputDirectory();

		var input = new CreateChangelogArguments
		{
			Title = "Known memory leak",
			Type = "known-issue",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = outputDir,
			IsNote = true,
			ExtractReleaseNotes = false
		};

		var result = await service.CreateNote(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yml");
		files.Should().HaveCount(1);
		var content = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		content.Should().NotContain("CI injected description that should be suppressed");
	}
}
