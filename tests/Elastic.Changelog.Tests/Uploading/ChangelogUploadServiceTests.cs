// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Changelog.Tests.Changelogs;
using Elastic.Changelog.Uploading;
using Elastic.Documentation.Integrations.S3;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Uploading;

public class ChangelogUploadServiceTests : IDisposable
{
	private readonly MockFileSystem _fileSystem = new();
	private readonly ChangelogUploadService _service;
	private readonly TestDiagnosticsCollector _collector;
	private readonly string _changelogDir;

	public ChangelogUploadServiceTests(ITestOutputHelper output)
	{
		_service = new ChangelogUploadService(NullLoggerFactory.Instance, fileSystem: _fileSystem);
		_collector = new TestDiagnosticsCollector(output);
		_changelogDir = _fileSystem.Path.Join(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "changelog");
		_fileSystem.Directory.CreateDirectory(_changelogDir);
	}

	public void Dispose() => GC.SuppressFinalize(this);

	private string AddChangelog(string fileName, string yaml)
	{
		var path = _fileSystem.Path.Join(_changelogDir, fileName);
		_fileSystem.AddFile(path, new MockFileData(yaml));
		return path;
	}

	[Fact]
	public void DiscoverUploadTargets_SingleProduct_MapsToCorrectS3Key()
	{
		// language=yaml
		var path = AddChangelog("entry.yaml", """
			title: New feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			  - "100"
			""");

		var targets = _service.DiscoverUploadTargets(_collector, _changelogDir);

		targets.Should().HaveCount(1);
		targets[0].LocalPath.Should().Be(path);
		targets[0].S3Key.Should().Be("elasticsearch/changelogs/entry.yaml");
		_collector.Errors.Should().Be(0);
	}

	[Fact]
	public void DiscoverUploadTargets_MultipleProducts_CreatesTargetPerProduct()
	{
		// language=yaml
		AddChangelog("fix.yaml", """
			title: Cross-product fix
			type: bug-fix
			products:
			  - product: elasticsearch
			    target: 9.2.0
			  - product: kibana
			    target: 9.2.0
			prs:
			  - "200"
			""");

		var targets = _service.DiscoverUploadTargets(_collector, _changelogDir);

		targets.Should().HaveCount(2);
		targets.Should().Contain(t => t.S3Key == "elasticsearch/changelogs/fix.yaml");
		targets.Should().Contain(t => t.S3Key == "kibana/changelogs/fix.yaml");
	}

	[Fact]
	public void DiscoverUploadTargets_InvalidProductName_SkipsWithWarning()
	{
		// language=yaml
		AddChangelog("bad.yaml", """
			title: Bad product
			type: feature
			products:
			  - product: "../traversal"
			prs:
			  - "300"
			""");

		var targets = _service.DiscoverUploadTargets(_collector, _changelogDir);

		targets.Should().BeEmpty();
		_collector.Warnings.Should().BeGreaterThan(0);
	}

	[Fact]
	public void DiscoverUploadTargets_NoProducts_ReturnsEmpty()
	{
		// language=yaml
		AddChangelog("noproducts.yaml", """
			title: No products
			type: feature
			prs:
			  - "400"
			""");

		var targets = _service.DiscoverUploadTargets(_collector, _changelogDir);

		targets.Should().BeEmpty();
		_collector.Errors.Should().Be(0);
	}

	[Fact]
	public void DiscoverUploadTargets_EmptyDirectory_ReturnsEmpty()
	{
		var targets = _service.DiscoverUploadTargets(_collector, _changelogDir);

		targets.Should().BeEmpty();
	}

	[Fact]
	public void DiscoverUploadTargets_MixedValidAndInvalidProducts_FiltersCorrectly()
	{
		// language=yaml
		AddChangelog("mixed.yaml", """
			title: Mixed products
			type: feature
			products:
			  - product: elasticsearch
			  - product: "bad product with spaces"
			  - product: kibana
			prs:
			  - "500"
			""");

		var targets = _service.DiscoverUploadTargets(_collector, _changelogDir);

		targets.Should().HaveCount(2);
		targets.Should().Contain(t => t.S3Key == "elasticsearch/changelogs/mixed.yaml");
		targets.Should().Contain(t => t.S3Key == "kibana/changelogs/mixed.yaml");
		_collector.Warnings.Should().BeGreaterThan(0);
	}

	[Fact]
	public void DiscoverUploadTargets_MultipleFiles_DiscoversBoth()
	{
		// language=yaml
		AddChangelog("first.yaml", """
			title: First
			type: feature
			products:
			  - product: elasticsearch
			prs:
			  - "1"
			""");
		// language=yaml
		AddChangelog("second.yaml", """
			title: Second
			type: bug-fix
			products:
			  - product: kibana
			prs:
			  - "2"
			""");

		var targets = _service.DiscoverUploadTargets(_collector, _changelogDir);

		targets.Should().HaveCount(2);
		targets.Should().Contain(t => t.S3Key == "elasticsearch/changelogs/first.yaml");
		targets.Should().Contain(t => t.S3Key == "kibana/changelogs/second.yaml");
	}

	[Fact]
	public void DiscoverUploadTargets_ProductWithHyphensAndUnderscores_Accepted()
	{
		// language=yaml
		AddChangelog("hyphen.yaml", """
			title: Hyphenated
			type: feature
			products:
			  - product: elastic-agent
			  - product: cloud_hosted
			prs:
			  - "600"
			""");

		var targets = _service.DiscoverUploadTargets(_collector, _changelogDir);

		targets.Should().HaveCount(2);
		targets.Should().Contain(t => t.S3Key == "elastic-agent/changelogs/hyphen.yaml");
		targets.Should().Contain(t => t.S3Key == "cloud_hosted/changelogs/hyphen.yaml");
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().Be(0);
	}
}
