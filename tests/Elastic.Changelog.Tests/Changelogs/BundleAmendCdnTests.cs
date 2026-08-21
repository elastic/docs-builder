// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests.Changelogs;

public class BundleAmendCdnTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	// language=yaml
	private const string ExistingEntry =
		"""
		title: Existing feature
		type: feature
		products:
		  - product: elasticsearch
		    target: 9.3.0
		""";

	// language=yaml
	private const string LateEntry =
		"""
		title: Late addition
		type: enhancement
		products:
		  - product: elasticsearch
		    target: 9.3.0
		""";

	// language=yaml
	private const string ChangedEntry =
		"""
		title: Changed on CDN
		type: feature
		products:
		  - product: elasticsearch
		    target: 9.3.0
		""";

	[Fact]
	public async Task Amend_Remove_CdnPath_NoLocalFile_ExcludesByChecksum()
	{
		var bundlePath = await WriteParentBundleAsync("existing.yaml", ExistingEntry);
		var handler = CdnHandler(("existing.yaml", ExistingEntry));
		var service = ServiceWithCdn(handler);

		var result = await service.AmendBundle(Collector, new AmendBundleArguments
		{
			BundlePath = bundlePath,
			RemoveFiles = ["/changelog/elastic/elasticsearch/main/existing.yaml"]
		}, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		handler.RequestedPaths.Should().Contain("/changelog/elastic/elasticsearch/main/registry.json");
		handler.RequestedPaths.Should().Contain(p => p.EndsWith("/existing.yaml", StringComparison.Ordinal));
		var amend = await ReadSingleAmendAsync(bundlePath);
		amend.Should().Contain("exclude-entries:");
		amend.Should().Contain("name: existing.yaml");
	}

	[Fact]
	public async Task Amend_Add_CdnBasename_NoLocalFile_EmbedsEntry()
	{
		var bundlePath = await WriteParentBundleAsync("existing.yaml", ExistingEntry);
		var handler = CdnHandler(("existing.yaml", ExistingEntry), ("late.yaml", LateEntry));
		var service = ServiceWithCdn(handler);

		var result = await service.AmendBundle(Collector, new AmendBundleArguments
		{
			BundlePath = bundlePath,
			AddFiles = ["late.yaml"]
		}, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		handler.RequestedPaths.Should().NotContain(p => p.EndsWith("/existing.yaml", StringComparison.Ordinal),
			"only requested names are fetched");
		var amend = await ReadSingleAmendAsync(bundlePath);
		amend.Should().Contain("title: Late addition");
		amend.Should().Contain("name: late.yaml");
	}

	[Fact]
	public async Task Amend_Add_CdnPoolMissingName_Fails()
	{
		var bundlePath = await WriteParentBundleAsync("existing.yaml", ExistingEntry);
		var service = ServiceWithCdn(CdnHandler(("existing.yaml", ExistingEntry)));

		var result = await service.AmendBundle(Collector, new AmendBundleArguments
		{
			BundlePath = bundlePath,
			AddFiles = ["never-uploaded.yaml"]
		}, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("never-uploaded.yaml") && d.Message.Contains("CDN pool"));
	}

	[Fact]
	public async Task Amend_ForceLocal_DoesNotHitCdn()
	{
		var bundlePath = await WriteParentBundleAsync("existing.yaml", ExistingEntry);
		var localDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(localDir);
		var localFile = FileSystem.Path.Join(localDir, "late.yaml");
		await FileSystem.File.WriteAllTextAsync(localFile, LateEntry, TestContext.Current.CancellationToken);

		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
		var service = ServiceWithCdn(handler);

		var result = await service.AmendBundle(Collector, new AmendBundleArguments
		{
			BundlePath = bundlePath,
			AddFiles = [localFile],
			ForceLocal = true
		}, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		handler.RequestedPaths.Should().BeEmpty("--force-local must not reach the CDN");
		var amend = await ReadSingleAmendAsync(bundlePath);
		amend.Should().Contain("title: Late addition");
	}

	[Fact]
	public async Task Amend_Remove_CdnChecksumMismatch_WithoutForce_Fails()
	{
		var bundlePath = await WriteParentBundleAsync("existing.yaml", ExistingEntry);
		var service = ServiceWithCdn(CdnHandler(("existing.yaml", ChangedEntry)));

		var result = await service.AmendBundle(Collector, new AmendBundleArguments
		{
			BundlePath = bundlePath,
			RemoveFiles = ["existing.yaml"]
		}, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("different checksum"));
	}

	[Fact]
	public async Task Amend_Remove_InferredEntry_ForceWithoutDummy_ExcludesByName()
	{
		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);
		var bundlePath = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		await FileSystem.File.WriteAllTextAsync(bundlePath, """
			products:
			- product: elasticsearch
			  target: 9.3.0
			  repo: elasticsearch
			  owner: elastic
			entries:
			- file:
			    name: 300.yaml
			    checksum: inferred-placeholder
			  type: enhancement
			  title: Inferred from PR
			""", TestContext.Current.CancellationToken);

		var handler = CdnHandler(("existing.yaml", ExistingEntry));
		var service = ServiceWithCdn(handler);

		var result = await service.AmendBundle(Collector, new AmendBundleArguments
		{
			BundlePath = bundlePath,
			RemoveFiles = ["300.yaml"],
			Force = true
		}, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		handler.RequestedPaths.Should().Contain("/changelog/elastic/elasticsearch/main/registry.json");
		handler.RequestedPaths.Should().NotContain(p => p.EndsWith("/300.yaml", StringComparison.Ordinal));
		var amend = await ReadSingleAmendAsync(bundlePath);
		amend.Should().Contain("name: 300.yaml");
		amend.Should().Contain("exclude-entries:");
	}

	private ChangelogBundleAmendService ServiceWithCdn(StubHandler handler)
	{
		var fetcher = new CdnChangelogEntryFetcher(LoggerFactory, handler, sleep: (_, _) => Task.CompletedTask);
		return new ChangelogBundleAmendService(LoggerFactory, FileSystem, entryFetcher: fetcher);
	}

	private async Task<string> WriteParentBundleAsync(string fileName, string changelogYaml)
	{
		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);
		var bundlePath = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		var checksum = ComputeSha1(changelogYaml);
		await FileSystem.File.WriteAllTextAsync(bundlePath, $"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			  repo: elasticsearch
			  owner: elastic
			entries:
			- file:
			    name: {fileName}
			    checksum: {checksum}
			  type: feature
			  title: Existing feature
			""", TestContext.Current.CancellationToken);
		return bundlePath;
	}

	private async Task<string> ReadSingleAmendAsync(string bundlePath)
	{
		var amendFiles = ChangelogBundleAmendService.DiscoverAmendFiles(FileSystem, bundlePath);
		amendFiles.Should().ContainSingle();
		return await FileSystem.File.ReadAllTextAsync(amendFiles[0], TestContext.Current.CancellationToken);
	}

	private static StubHandler CdnHandler(params (string FileName, string Yaml)[] entries)
	{
		var filesJson = string.Join(", ", entries.Select(e => $"{{ \"file\": \"{e.FileName}\" }}"));
		var registry = $"{{ \"schema_version\": 1, \"product\": \"elasticsearch\", \"bundles\": [ {filesJson} ] }}";
		var byName = entries.ToDictionary(e => e.FileName, e => e.Yaml, StringComparer.Ordinal);
		return new StubHandler(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path.EndsWith("/registry.json", StringComparison.Ordinal))
				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(registry, System.Text.Encoding.UTF8, "application/json")
				};
			foreach (var (fileName, yaml) in byName)
			{
				if (path.EndsWith("/" + fileName, StringComparison.Ordinal))
					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent(yaml, System.Text.Encoding.UTF8, "text/yaml")
					};
			}
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});
	}

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		public List<string> RequestedPaths { get; } = [];

		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			RequestedPaths.Add(request.RequestUri!.AbsolutePath);
			return responder(request);
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
