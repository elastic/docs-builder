// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Uploading;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Integrations.S3;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// The full amend acceptance chain: <c>bundle-amend</c> materializes a self-contained amend
/// (parent products copied) → upload destination discovery includes the amend → the registry
/// records the amend's target → a <c>:version:</c>-filtered CDN fetch returns the amend and the
/// <c>exclude-entries</c> retraction (matched by <c>file</c> identity) applies.
/// </summary>
public class BundleAmendEndToEndTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	[Fact]
	public async Task AmendLifecycle_FromCreationToVersionFilteredCdnConsumption()
	{
		var ct = TestContext.Current.CancellationToken;

		// -- Authoring fixtures -------------------------------------------------------------
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// language=yaml
		var retractedEntry =
			"""
			title: Retracted fix
			type: bug-fix
			products:
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			- "100"
			""";
		var retractedFile = FileSystem.Path.Join(changelogDir, "1-old.yaml");
		await FileSystem.File.WriteAllTextAsync(retractedFile, retractedEntry, ct);

		// language=yaml
		var addedEntry =
			"""
			title: Late addition
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			- "200"
			""";
		var addedFile = FileSystem.Path.Join(changelogDir, "2-late.yaml");
		await FileSystem.File.WriteAllTextAsync(addedFile, addedEntry, ct);

		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);
		var parentPath = FileSystem.Path.Join(bundleDir, "elasticsearch-9.3.0.yaml");
		// A resolved parent whose entry carries a file identity, so the retraction below can match it.
		// language=yaml
		await FileSystem.File.WriteAllTextAsync(parentPath, $"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			  lifecycle: ga
			  repo: elasticsearch
			  owner: elastic
			entries:
			- file:
			    name: 1-old.yaml
			    checksum: {ComputeSha1(retractedEntry)}
			  type: bug-fix
			  title: Retracted fix
			  prs:
			  - "100"
			""", ct);

		// -- 1. bundle-amend materializes a self-contained amend -----------------------------
		var amendService = new ChangelogBundleAmendService(LoggerFactory, FileSystem);
		var amendResult = await amendService.AmendBundle(Collector, new AmendBundleArguments
		{
			BundlePath = parentPath,
			AddFiles = [addedFile],
			RemoveFiles = [retractedFile]
		}, ct);

		amendResult.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var amendFiles = ChangelogBundleAmendService.DiscoverAmendFiles(FileSystem, parentPath);
		amendFiles.Should().ContainSingle();
		var amend = ReleaseNotesSerialization.DeserializeBundle(await FileSystem.File.ReadAllTextAsync(amendFiles[0], ct));
		amend.Products.Should().ContainSingle("the amend copies the parent's complete products");
		amend.Products[0].Target.Should().Be("9.3.0");
		amend.Products[0].Repo.Should().Be("elasticsearch");
		amend.Products[0].Owner.Should().Be("elastic");

		// -- 2. upload destination discovery includes the amend ------------------------------
		var s3Client = A.Fake<IAmazonS3>();
		var uploadCollector = new TestDiagnosticsCollector(Output);
		var uploadService = new ChangelogUploadService(NullLoggerFactory.Instance, fileSystem: FileSystem, s3Client: s3Client);
		var targets = uploadService.DiscoverBundleUploadTargets(uploadCollector, bundleDir);

		targets.Select(t => t.S3Key).Should().BeEquivalentTo(
			"bundle/elasticsearch/elasticsearch-9.3.0.yaml",
			"bundle/elasticsearch/elasticsearch-9.3.0.amend-1.yaml");
		uploadCollector.Errors.Should().Be(0);
		uploadCollector.Warnings.Should().Be(0);

		// -- 3. the registry records the amend's target --------------------------------------
		var puts = new List<PutObjectRequest>();
		A.CallTo(() => s3Client.GetObjectAsync(A<GetObjectRequest>._, A<CancellationToken>._))
			.Throws(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });
		A.CallTo(() => s3Client.PutObjectAsync(A<PutObjectRequest>._, A<CancellationToken>._))
			.Invokes((PutObjectRequest r, CancellationToken _) => puts.Add(r))
			.Returns(new PutObjectResponse());

		var registryCollector = new TestDiagnosticsCollector(Output);
		var etagCalculator = new S3EtagCalculator(NullLoggerFactory.Instance, FileSystem);
		var builder = new RegistryBuilder(NullLoggerFactory.Instance, FileSystem, s3Client, etagCalculator, "test-bucket");
		var refresh = await builder.RefreshAsync(registryCollector, targets, ct);

		refresh.Updated.Should().Be(1);
		var registryPut = puts.Single(p => p.Key == "bundle/elasticsearch/registry.json");
		registryPut.ContentBody.Should().Contain("elasticsearch-9.3.0.amend-1.yaml");

		// -- 4. :version:-filtered CDN fetch returns the amend and applies the retraction ----
		using var handler = new StubHandler(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path.EndsWith("/registry.json", StringComparison.Ordinal))
				return Response(registryPut.ContentBody, "application/json");

			var fileName = path[(path.LastIndexOf('/') + 1)..];
			var localPath = FileSystem.Path.Join(bundleDir, fileName);
			return FileSystem.File.Exists(localPath)
				? Response(FileSystem.File.ReadAllText(localPath), "text/yaml")
				: new HttpResponseMessage(HttpStatusCode.NotFound);
		});

		var fetchErrors = new List<string>();
		var fetchWarnings = new List<string>();
		using var fetcher = new CdnChangelogFetcher(NullLoggerFactory.Instance, FileSystem, handler);
		var bundles = await fetcher.FetchAsync(
			new Uri("https://cdn.example"),
			"elasticsearch",
			version: "9.3.0",
			fetchErrors.Add,
			fetchWarnings.Add,
			ct);

		fetchErrors.Should().BeEmpty();
		fetchWarnings.Should().BeEmpty();
		bundles.Should().ContainSingle("the amend merges into its parent");
		bundles[0].Version.Should().Be("9.3.0");
		bundles[0].Entries.Select(e => e.Title).Should().BeEquivalentTo(
			["Late addition"],
			"the added entry is present and the file-identity retraction removed the original entry");
	}

	private static HttpResponseMessage Response(string body, string mediaType) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, mediaType) };

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(responder(request));
	}
}
