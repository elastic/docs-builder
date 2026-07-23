// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Guards against publishing a bundle whose bundle-level product target disagrees with the targets its
/// own entries declare for the same product. This reproduces the class of defect where a release
/// version was passed as <c>2027-07-20</c> while every entry (and the release date) said
/// <c>2026-07-20</c>, which silently rendered the entries under the wrong date.
/// </summary>
public class BundleTargetConsistencyTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	private ChangelogBundlingService Service => new(LoggerFactory, null, FileSystem);

	private string WriteChangelogDir(params (string fileName, string content)[] files)
	{
		var dir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(dir);
		foreach (var (fileName, content) in files)
			FileSystem.File.WriteAllText(FileSystem.Path.Join(dir, fileName), content);
		return dir;
	}

	private string NewOutputPath()
	{
		var outputPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(outputPath)!);
		return outputPath;
	}

	[Fact]
	public async Task BundleTarget_DivergesFromEntryTarget_FailsWithError()
	{
		// language=yaml
		var entry =
			"""
			title: Prevent overriding inference API secret_parameters
			type: breaking-change
			products:
			  - product: cloud-serverless
			    target: 2026-07-20
			prs:
			  - https://github.com/elastic/elasticsearch/pull/153309
			""";

		var changelogDir = WriteChangelogDir(("153309.yaml", entry));
		var outputPath = NewOutputPath();

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			All = true,
			Output = outputPath,
			// Release version typed one year off — the real-world defect being guarded against.
			OutputProducts = [new ProductArgument { Product = "cloud-serverless", Target = "2027-07-20" }]
		};

		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse("a bundle target inconsistent with its entry targets must not be produced");
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("2027-07-20") &&
			d.Message.Contains("2026-07-20") &&
			d.Message.Contains("cloud-serverless"));
		FileSystem.File.Exists(outputPath).Should().BeFalse("the inconsistent bundle must not be written");
	}

	[Fact]
	public async Task BundleTarget_CoarserPrefixOfEntryTarget_Succeeds()
	{
		// A monthly rollup: bundle target is the coarser month, entries carry a specific day within it.
		// language=yaml
		var entry =
			"""
			title: Faster hosted search
			type: feature
			products:
			  - product: cloud-hosted
			    target: 2026-05-15
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var changelogDir = WriteChangelogDir(("100-feature.yaml", entry));
		var outputPath = NewOutputPath();

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			All = true,
			Output = outputPath,
			OutputProducts = [new ProductArgument { Product = "cloud-hosted", Target = "2026-05" }]
		};

		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"a coarser bundle target that is a prefix of the entry target is valid. Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
		FileSystem.File.Exists(outputPath).Should().BeTrue();
	}

	[Fact]
	public async Task BundleTarget_MatchesEntryTarget_Succeeds()
	{
		// language=yaml
		var entry =
			"""
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.5.0
			    lifecycle: ga
			prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			""";

		var changelogDir = WriteChangelogDir(("100-feature.yaml", entry));
		var outputPath = NewOutputPath();

		var input = new BundleChangelogsArguments
		{
			Directory = changelogDir,
			All = true,
			Output = outputPath,
			OutputProducts = [new ProductArgument { Product = "elasticsearch", Target = "9.5.0" }]
		};

		var result = await Service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"matching targets are valid. Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
		FileSystem.File.Exists(outputPath).Should().BeTrue();
	}
}
