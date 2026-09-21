// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Creation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Tests.Changelogs.Create;

public class ChangelogUnpackTests(ITestOutputHelper output) : CreateChangelogTestBase(output)
{
	private const string ConfigYaml =
		"""
		pivot:
		  types:
		    feature: "type:feature"
		    enhancement: "type:enhancement"
		    bug-fix: "type:bug"
		    breaking-change:
		    known-issue:
		lifecycles:
		  - preview
		  - beta
		  - ga
		""";

	[Fact]
	public async Task Unpack_PrEntry_WritesAddNamedFileAndStripsPrivateSentinel()
	{
		var configPath = await CreateConfigDirectory(ConfigYaml);
		var outputDir = CreateOutputDirectory();
		var bundlePath = await WriteBundle(
			"""
			products:
			- product: cloud-serverless
			  target: 2026-09-08
			  repo: elasticsearch-serverless
			  owner: elastic
			entries:
			- file:
			    name: 7606.yaml
			    checksum: 88dfd443adb87e70e71e7d8cb8417e4de1b91268
			  type: enhancement
			  title: Add ECS user.domain to serverless audit logs
			  products:
			  - product: cloud-serverless
			  areas:
			  - Security
			  prs:
			  - '# PRIVATE: https://github.com/elastic/elasticsearch-serverless/pull/7606'
			"""
		);

		var result = await Unpack(bundlePath, configPath, outputDir);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var written = FileSystem.Path.Join(outputDir, "7606.yaml");
		FileSystem.File.Exists(written).Should().BeTrue();
		var yaml = await FileSystem.File.ReadAllTextAsync(written, TestContext.Current.CancellationToken);
		yaml.Should().Contain("title: Add ECS user.domain to serverless audit logs");
		yaml.Should().Contain("type: enhancement");
		yaml.Should().Contain("https://github.com/elastic/elasticsearch-serverless/pull/7606");
		yaml.Should().NotContain("# PRIVATE:");
		yaml.Should().NotContain("checksum:");
		yaml.Should().Contain("##### Required fields");

		var parsed = ReleaseNotesSerialization.DeserializeEntry(yaml);
		parsed.Title.Should().Be("Add ECS user.domain to serverless audit logs");
		parsed.Type.Should().Be(ChangelogEntryType.Enhancement);
		parsed.Prs.Should().ContainSingle().Which.Should().Be("https://github.com/elastic/elasticsearch-serverless/pull/7606");

		ComputeSha1(yaml).Should().NotBe("88dfd443adb87e70e71e7d8cb8417e4de1b91268");
	}

	[Fact]
	public async Task Unpack_VersionedEntryWithoutPrs_WritesNoteFile()
	{
		var configPath = await CreateConfigDirectory(ConfigYaml);
		var outputDir = CreateOutputDirectory();
		var bundlePath = await WriteBundle(
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			  repo: elasticsearch
			  owner: elastic
			entries:
			- file:
			    name: note-known-issue.yaml
			    checksum: deadbeef
			  type: known-issue
			  title: Alerts are not generated when flapping is off
			  products:
			  - product: elasticsearch
			    versions:
			    - 9.3.0
			"""
		);

		var result = await Unpack(bundlePath, configPath, outputDir);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Warning && d.Message.Contains("note-alerts-are-not-generated-when-flapping-is-off.yml"));

		var files = FileSystem.Directory.GetFiles(outputDir, "note-*.yml");
		files.Should().ContainSingle();
		var yaml = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		var parsed = ReleaseNotesSerialization.DeserializeEntry(yaml);
		parsed.Type.Should().Be(ChangelogEntryType.KnownIssue);
		parsed.Products.Should().ContainSingle().Which.Versions.Should().Contain("9.3.0");
	}

	[Fact]
	public async Task Unpack_MultiPrEntry_WritesSingleFileNotOnePerPr()
	{
		var configPath = await CreateConfigDirectory(ConfigYaml);
		var outputDir = CreateOutputDirectory();
		var bundlePath = await WriteBundle(
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			  repo: elasticsearch
			  owner: elastic
			entries:
			- file:
			    name: 100-200.yaml
			    checksum: abc
			  type: bug-fix
			  title: Fix spanning two PRs
			  products:
			  - product: elasticsearch
			  prs:
			  - https://github.com/elastic/elasticsearch/pull/100
			  - https://github.com/elastic/elasticsearch/pull/200
			"""
		);

		var result = await Unpack(bundlePath, configPath, outputDir);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		FileSystem.Directory.GetFiles(outputDir, "*.yaml").Should().ContainSingle().Which.Should().EndWith("100-200.yaml");
	}

	[Fact]
	public async Task Unpack_ParentMergesAmendAdditionAndExclusion()
	{
		var configPath = await CreateConfigDirectory(ConfigYaml);
		var outputDir = CreateOutputDirectory();
		var dir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(dir);
		var parent = FileSystem.Path.Join(dir, "release.yaml");
		await FileSystem.File.WriteAllTextAsync(
			parent,
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			  repo: elasticsearch
			  owner: elastic
			entries:
			- file:
			    name: 111.yaml
			    checksum: keep
			  type: feature
			  title: Kept feature
			  products:
			  - product: elasticsearch
			  prs:
			  - https://github.com/elastic/elasticsearch/pull/111
			- file:
			    name: 222.yaml
			    checksum: drop
			  type: bug-fix
			  title: Retracted fix
			  products:
			  - product: elasticsearch
			  prs:
			  - https://github.com/elastic/elasticsearch/pull/222
			""",
			TestContext.Current.CancellationToken
		);
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Join(dir, "release.amend-1.yaml"),
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			  repo: elasticsearch
			  owner: elastic
			exclude-entries:
			- file:
			    name: 222.yaml
			    checksum: drop
			entries:
			- file:
			    name: 333.yaml
			    checksum: add
			  type: enhancement
			  title: Late addition
			  products:
			  - product: elasticsearch
			  prs:
			  - https://github.com/elastic/elasticsearch/pull/333
			""",
			TestContext.Current.CancellationToken
		);

		var result = await Unpack(parent, configPath, outputDir);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		var names = FileSystem.Directory.GetFiles(outputDir, "*.yaml").Select(FileSystem.Path.GetFileName).OrderBy(n => n).ToArray();
		names.Should().Equal("111.yaml", "333.yaml");
	}

	[Fact]
	public async Task Unpack_AmendSidecar_WritesAdditionsOnly()
	{
		var configPath = await CreateConfigDirectory(ConfigYaml);
		var outputDir = CreateOutputDirectory();
		var dir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(dir);
		var amend = FileSystem.Path.Join(dir, "release.amend-1.yaml");
		await FileSystem.File.WriteAllTextAsync(
			amend,
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			  repo: elasticsearch
			  owner: elastic
			exclude-entries:
			- file:
			    name: 222.yaml
			    checksum: drop
			entries:
			- file:
			    name: 333.yaml
			    checksum: add
			  type: enhancement
			  title: Late addition
			  products:
			  - product: elasticsearch
			  prs:
			  - https://github.com/elastic/elasticsearch/pull/333
			""",
			TestContext.Current.CancellationToken
		);

		var result = await Unpack(amend, configPath, outputDir);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		FileSystem.Directory.GetFiles(outputDir, "*.yaml").Select(FileSystem.Path.GetFileName).Should().Equal("333.yaml");
	}

	[Fact]
	public async Task Unpack_ExcludeOnlyAmend_WritesNothing()
	{
		var configPath = await CreateConfigDirectory(ConfigYaml);
		var outputDir = CreateOutputDirectory();
		var dir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(dir);
		var amend = FileSystem.Path.Join(dir, "release.amend-2.yaml");
		await FileSystem.File.WriteAllTextAsync(
			amend,
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			  repo: elasticsearch
			  owner: elastic
			exclude-entries:
			- file:
			    name: 222.yaml
			    checksum: drop
			""",
			TestContext.Current.CancellationToken
		);

		var result = await Unpack(amend, configPath, outputDir);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		FileSystem.Directory.Exists(outputDir).Should().BeFalse();
	}

	[Fact]
	public async Task Unpack_ScrubbedEntryWithoutPrsOrVersions_Fails()
	{
		var configPath = await CreateConfigDirectory(ConfigYaml);
		var outputDir = CreateOutputDirectory();
		var bundlePath = await WriteBundle(
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			  repo: elasticsearch-serverless
			  owner: elastic
			entries:
			- file:
			    name: 7606.yaml
			    checksum: abc
			  type: enhancement
			  title: Scrubbed private change
			  products:
			  - product: elasticsearch
			"""
		);

		var result = await Unpack(bundlePath, configPath, outputDir);

		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("scrubbed during changelog upload"));
		FileSystem.Directory.Exists(outputDir).Should().BeFalse();
	}

	private async Task<bool> Unpack(string bundlePath, string configPath, string outputDir)
	{
		var service = new ChangelogUnpackService(LoggerFactory, FileSystem, ConfigurationContext);
		return await service.UnpackBundle(
			Collector,
			new UnpackBundleArguments { BundleFile = bundlePath, Config = configPath, Output = outputDir, Concise = false },
			TestContext.Current.CancellationToken
		);
	}

	private async Task<string> WriteBundle(string yaml)
	{
		var dir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(dir);
		var path = FileSystem.Path.Join(dir, "bundle.yaml");
		await FileSystem.File.WriteAllTextAsync(path, yaml, TestContext.Current.CancellationToken);
		return path;
	}
}
