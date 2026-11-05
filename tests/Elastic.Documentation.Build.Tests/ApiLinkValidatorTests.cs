// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Build.Tests;

public class ApiLinkValidatorTests
{
	private static readonly IReadOnlySet<string> Generated = new HashSet<string>
	{
		"/docs/api/doc/kibana",
		"/docs/api/doc/kibana/v8/operation/operation-upgrade",
		"/docs/api/doc/kibana/operation/operation-alias",
		"/docs/api/doc/kibana-serverless/operation/operation-live",
		"/docs/api/doc/cloud-serverless.yaml",
		"/docs/api/doc/kibana/kibana-api-overview"
	};

	private static ApiLinkReference Link(string url, string file = "a.md", int line = 1) => new(url, file, line, 1, url.Length);

	[Test]
	public void FindBroken_ExistingPage_IsNotReported() =>
		ApiLinkValidator.FindBroken([Link("https://www.elastic.co/docs/api/doc/kibana/")], Generated).Should().BeEmpty();

	[Test]
	public void FindBroken_PageOnlyInOlderVersion_ReportsMainTreeLink()
	{
		var broken = ApiLinkValidator.FindBroken(
			[
				Link("https://www.elastic.co/docs/api/doc/kibana/operation/operation-upgrade"),
				Link("https://www.elastic.co/docs/api/doc/kibana/v8/operation/operation-upgrade#responses")
			],
			Generated
		);
		broken.Should().ContainSingle().Which.Url.Should().EndWith("/kibana/operation/operation-upgrade");
	}

	[Test]
	public void FindBroken_OldBumpShRootWithLivePage_IsNotReported() =>
		ApiLinkValidator
			.FindBroken(
				[
					Link("https://www.elastic.co/docs/api/doc/serverless/operation/operation-live"),
					Link("https://www.elastic.co/docs/api/doc/kibana/topic/topic-kibana-spaces")
				],
				Generated
			)
			.Should()
			.BeEmpty();

	[Test]
	public void FindBroken_OldBumpShRootSpecDownload_ResolvesToTheNewRootDownload() =>
		ApiLinkValidator
			.FindBroken([Link("https://www.elastic.co/docs/api/doc/elastic-cloud-serverless.yaml")], Generated)
			.Should()
			.BeEmpty();

	[Test]
	public void FindBroken_OldBumpShRootWithMissingPage_IsStillReported() =>
		ApiLinkValidator
			.FindBroken([Link("https://www.elastic.co/docs/api/doc/serverless/operation/operation-removed")], Generated)
			.Should()
			.ContainSingle();

	[Test]
	public void FindBroken_GoneBumpShRoot_IsReported() =>
		ApiLinkValidator
			.FindBroken([Link("https://www.elastic.co/docs/api/doc/observability-serverless/operation/operation-x")], Generated)
			.Should()
			.ContainSingle();

	[Test]
	public void FindBroken_AliasUrl_Resolves() =>
		ApiLinkValidator
			.FindBroken([Link("https://www.elastic.co/docs/api/doc/kibana/operation/operation-alias")], Generated)
			.Should()
			.BeEmpty();

	[Test]
	public void FindBroken_MultipleBroken_AreOrderedByUrlThenSource()
	{
		var broken = ApiLinkValidator.FindBroken(
			[
				Link("https://www.elastic.co/docs/api/doc/kibana/zzz", "b.md"),
				Link("https://www.elastic.co/docs/api/doc/kibana/aaa", "b.md"),
				Link("https://www.elastic.co/docs/api/doc/kibana/aaa", "a.md")
			],
			Generated
		);
		broken.Select(b => (b.Url.Split('/')[^1], b.File)).Should().Equal(("aaa", "a.md"), ("aaa", "b.md"), ("zzz", "b.md"));
	}

	private static (DiagnosticsCollector Collector, MockFileSystem FileSystem) Setup(params ApiLinkReference[] links)
	{
		var collector = new DiagnosticsCollector([]);
		foreach (var link in links)
			collector.EmitApiLink(link);
		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/out");
		return (collector, fileSystem);
	}

	[Test]
	public async Task ValidateAsync_BrokenLink_WarnsAtItsPositionAndWritesTheReport()
	{
		var (collector, fileSystem) = Setup(
			new ApiLinkReference("https://www.elastic.co/docs/api/doc/kibana/", "a.md", 4, 7, 40),
			new ApiLinkReference("https://www.elastic.co/docs/api/doc/kibana/operation/missing#x", "b.md", 9, 3, 55)
		);

		var path = await ApiLinkValidator.ValidateAsync(
			collector,
			Generated,
			fileSystem,
			"/out",
			TestContext.Current!.Execution.CancellationToken
		);

		collector.Warnings.Should().Be(1);
		path.Should().EndWith("api-link-report.json");
		var bytes = await fileSystem.File.ReadAllBytesAsync(path!);
		bytes.Take(3).Should().NotEqual([(byte)0xEF, (byte)0xBB, (byte)0xBF], "the report is plain UTF-8 without a BOM");
		using var report = JsonDocument.Parse(bytes);
		report.RootElement.GetProperty("checked").GetInt32().Should().Be(2);
		var broken = report.RootElement.GetProperty("broken").EnumerateArray().Single();
		broken.GetProperty("source").GetString().Should().Be("b.md");
		broken.GetProperty("line").GetInt32().Should().Be(9);
	}

	[Test]
	public async Task ValidateAsync_SameLinkRecordedByEveryIncludingPage_ReportsOnce()
	{
		var link = new ApiLinkReference("https://www.elastic.co/docs/api/doc/kibana/gone", "_snippets/s.md", 2, 1, 30);
		var (collector, fileSystem) = Setup(link, link, link);

		_ = await ApiLinkValidator.ValidateAsync(
			collector,
			Generated,
			fileSystem,
			"/out",
			TestContext.Current!.Execution.CancellationToken
		);

		collector.Warnings.Should().Be(1);
		using var report = JsonDocument.Parse(await fileSystem.File.ReadAllTextAsync("/out/api-link-report.json"));
		report.RootElement.GetProperty("checked").GetInt32().Should().Be(1);
	}

	[Test]
	public async Task ValidateAsync_NoGeneratedPages_SkipsWithoutWarningsOrReport()
	{
		var (collector, fileSystem) = Setup(new ApiLinkReference("https://www.elastic.co/docs/api/doc/kibana/gone", "a.md", 1, 1, 30));

		var path = await ApiLinkValidator.ValidateAsync(
			collector,
			new HashSet<string>(),
			fileSystem,
			"/out",
			TestContext.Current!.Execution.CancellationToken
		);

		path.Should().BeNull();
		collector.Warnings.Should().Be(0);
		fileSystem.File.Exists("/out/api-link-report.json").Should().BeFalse();
	}

	[Test]
	public async Task ValidateAsync_NoRecordedLinks_ReturnsNull()
	{
		var (collector, fileSystem) = Setup();

		var path = await ApiLinkValidator.ValidateAsync(
			collector,
			Generated,
			fileSystem,
			"/out",
			TestContext.Current!.Execution.CancellationToken
		);

		path.Should().BeNull();
	}
}
