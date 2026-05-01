// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Creation;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Configuration;
using FakeItEasy;

namespace Elastic.Changelog.Tests.Changelogs.Create;

/// <summary>
/// Tests promotion-report parsing feeding <see cref="ChangelogCreationService"/> (same expansion
/// <c>changelog add --report</c> performs before creation).
/// </summary>
public class AddReportOptionTests(ITestOutputHelper output) : CreateChangelogTestBase(output)
{
	[Fact]
	public async Task CreateChangelog_FromPromotionReportHtmlFile_CreatesOneYamlPerPr()
	{
		var html =
			"""
			<html><body>
			  <a href="https://github.com/elastic/elasticsearch/pull/7001">PR #7001</a>
			  <a href="https://github.com/elastic/elasticsearch/pull/7002">PR #7002</a>
			</body></html>
			""";
		var reportFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "promotion.html");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(reportFile)!);
		await FileSystem.File.WriteAllTextAsync(reportFile, html, TestContext.Current.CancellationToken);

		var pr1 = new GitHubPrInfo { Title = "First from report", Labels = ["type:feature"] };
		var pr2 = new GitHubPrInfo { Title = "Second from report", Labels = ["type:bug"] };
		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				A<string>.That.Contains("7001"),
				null,
				null,
				A<CancellationToken>._))
			.Returns(pr1);
		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				A<string>.That.Contains("7002"),
				null,
				null,
				A<CancellationToken>._))
			.Returns(pr2);

		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature: "type:feature"
			    bug-fix: "type:bug"
			    breaking-change:
			lifecycles:
			  - preview
			  - beta
			  - ga
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var parser = new PromotionReportParser(LoggerFactory, FileSystem);
		var prUrls = await parser.ParseReportToPrUrlsAsync(Collector, reportFile, TestContext.Current.CancellationToken);
		prUrls.Should().NotBeNull();
		prUrls!.Should().HaveCount(2);

		var service = CreateService();
		var input = new CreateChangelogArguments
		{
			Prs = prUrls,
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = CreateOutputDirectory(),
			UsePrNumber = true
		};

		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var outputDir = input.Output!;
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(2);
		Array.Sort(files, StringComparer.Ordinal);
		Path.GetFileName(files[0]).Should().Be("7001.yaml");
		Path.GetFileName(files[1]).Should().Be("7002.yaml");

		var yaml1 = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yaml1.Should().Contain("title: First from report");
		yaml1.Should().Contain("https://github.com/elastic/elasticsearch/pull/7001");
	}

	[Fact]
	public async Task PromotionReportParser_ReportFileMissing_EmitsError()
	{
		var missing = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "nope.html");
		var parser = new PromotionReportParser(LoggerFactory, FileSystem);

		var prUrls = await parser.ParseReportToPrUrlsAsync(Collector, missing, TestContext.Current.CancellationToken);

		prUrls.Should().BeNull();
		Collector.Errors.Should().BeGreaterThan(0);
	}
}
