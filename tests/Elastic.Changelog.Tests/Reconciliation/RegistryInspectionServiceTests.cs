// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Changelog.Reconciliation;
using Elastic.Documentation.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.ScopedFileSystem;

namespace Elastic.Changelog.Tests.Reconciliation;

[SuppressMessage("Usage", "CA1001:Types that own disposable fields should be disposable")]
public class RegistryInspectionServiceTests(ITestOutputHelper output)
{
	private readonly FakeS3Bucket _bucket = new();
	private readonly MockFileSystem _mockFileSystem = new(new MockFileSystemOptions
	{
		CurrentDirectory = Paths.WorkingDirectoryRoot.FullName
	});
	private readonly TestDiagnosticsCollector _collector = new(output);

	private ChangelogRegistryInspectionService Service =>
		new(NullLoggerFactory.Instance, _bucket.Client, FileSystemFactory.ScopeCurrentWorkingDirectory(_mockFileSystem));

	[Fact]
	public async Task Inspect_CleanScope_Succeeds()
	{
		// An empty scope with no manifest: nothing published, nothing to reconcile.
		var args = new ChangelogRegistryInspectArguments { S3BucketName = "private-bucket", Product = "elasticsearch" };

		var result = await Service.Inspect(_collector, args, TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
	}

	[Fact]
	public async Task Inspect_DivergedScope_ErrorsAndWritesSnapshot()
	{
		_ = _bucket.Seed("bundle/elasticsearch/9.3.0.yaml", "entries: []");
		var outPath = _mockFileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, "snapshot.json");
		var args = new ChangelogRegistryInspectArguments
		{
			S3BucketName = "private-bucket",
			Product = "elasticsearch",
			Out = outPath
		};

		var result = await Service.Inspect(_collector, args, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_bucket.Puts.Should().BeEmpty("inspection is read-only");

		var snapshot = JsonSerializer.Deserialize(
			_mockFileSystem.File.ReadAllText(outPath),
			RegistryStateJsonContext.Default.RegistryStateSnapshot);
		snapshot!.Divergences.Should().ContainSingle().Which.Kind.Should().Be(RegistryDivergenceKind.Missing);
	}

	[Fact]
	public async Task Inspect_MissingScopeSelection_Errors()
	{
		var args = new ChangelogRegistryInspectArguments { S3BucketName = "private-bucket" };

		var result = await Service.Inspect(_collector, args, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
	}

}
