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

public class BundleFilesFilterTests : ChangelogTestBase
{
	private ChangelogBundlingService ServiceWithConfig { get; }
	private readonly string _changelogDir;

	// language=yaml
	private const string EntryKeep =
		"""
		title: Keep me
		type: feature
		products:
		  - product: elasticsearch
		    target: 9.3.0
		    lifecycle: ga
		""";

	// language=yaml
	private const string EntrySkip =
		"""
		title: Skip me
		type: feature
		products:
		  - product: elasticsearch
		    target: 9.3.0
		    lifecycle: ga
		""";

	// language=yaml
	private const string EntryBugFix =
		"""
		title: Bug fix
		type: bug-fix
		products:
		  - product: elasticsearch
		    target: 9.3.0
		    lifecycle: ga
		""";

	public BundleFilesFilterTests(ITestOutputHelper output) : base(output)
	{
		ServiceWithConfig = new(LoggerFactory, ConfigurationContext, FileSystem);
		_changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(_changelogDir);
	}

	[Fact]
	public async Task Bundle_WithFiles_IncludesOnlyNamedEntries()
	{
		var keep = FileSystem.Path.Join(_changelogDir, "keep.yaml");
		var skip = FileSystem.Path.Join(_changelogDir, "skip.yaml");
		await FileSystem.File.WriteAllTextAsync(keep, EntryKeep, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(skip, EntrySkip, TestContext.Current.CancellationToken);

		var output = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Files = [keep],
			Output = output,
			ForceLocal = true
		};

		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		var bundle = await FileSystem.File.ReadAllTextAsync(output, TestContext.Current.CancellationToken);
		bundle.Should().Contain("name: keep.yaml");
		bundle.Should().NotContain("name: skip.yaml");
	}

	[Fact]
	public async Task Bundle_WithFiles_MissingFile_ReturnsError()
	{
		var missing = FileSystem.Path.Join(_changelogDir, "missing.yaml");
		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Files = [missing],
			Output = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml")
		};

		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("File does not exist"));
	}

	[Fact]
	public async Task Bundle_WithFilesAndPrs_ReturnsMutualExclusivityError()
	{
		var keep = FileSystem.Path.Join(_changelogDir, "keep.yaml");
		await FileSystem.File.WriteAllTextAsync(keep, EntryKeep, TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Files = [keep],
			Prs = ["https://github.com/elastic/elasticsearch/pull/1"],
			Output = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml")
		};

		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("Multiple filter options"));
	}

	[Fact]
	public async Task Bundle_WithPathListFile_IncludesListedEntries()
	{
		var keep = FileSystem.Path.Join(_changelogDir, "keep.yaml");
		var skip = FileSystem.Path.Join(_changelogDir, "skip.yaml");
		await FileSystem.File.WriteAllTextAsync(keep, EntryKeep, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(skip, EntrySkip, TestContext.Current.CancellationToken);

		var listFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "files.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(listFile)!);
		await FileSystem.File.WriteAllTextAsync(listFile, $"{keep}\n", TestContext.Current.CancellationToken);

		var output = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Files = [listFile],
			Output = output
		};

		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		var bundle = await FileSystem.File.ReadAllTextAsync(output, TestContext.Current.CancellationToken);
		bundle.Should().Contain("name: keep.yaml");
		bundle.Should().NotContain("name: skip.yaml");
	}

	[Fact]
	public async Task Bundle_WithProfile_PathListFile_FiltersCorrectly()
	{
		var configContent = $"""
			bundle:
			  directory: {_changelogDir}
			  profiles:
			    release:
			      output: "bundle.yaml"
			""";
		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var keep = FileSystem.Path.Join(_changelogDir, "keep.yaml");
		var skip = FileSystem.Path.Join(_changelogDir, "skip.yaml");
		await FileSystem.File.WriteAllTextAsync(keep, EntryKeep, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(skip, EntrySkip, TestContext.Current.CancellationToken);

		var listFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "files.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(listFile)!);
		await FileSystem.File.WriteAllTextAsync(listFile, "keep.yaml\n", TestContext.Current.CancellationToken);

		var expectedOutput = FileSystem.Path.Join(_changelogDir, "bundle.yaml");
		var input = new BundleChangelogsArguments
		{
			Config = configPath,
			Profile = "release",
			ProfileArgument = "9.3.0",
			ProfileReport = listFile
		};

		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		var bundle = await FileSystem.File.ReadAllTextAsync(expectedOutput, TestContext.Current.CancellationToken);
		bundle.Should().Contain("name: keep.yaml");
		bundle.Should().NotContain("name: skip.yaml");
	}

	[Fact]
	public async Task Bundle_WithProfile_MixedUrlsAndPaths_ReturnsError()
	{
		var configContent =
			"""
			bundle:
			  profiles:
			    release:
			      output: "bundle.yaml"
			""";
		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var listFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "mixed.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(listFile)!);
		await FileSystem.File.WriteAllTextAsync(
			listFile,
			"https://github.com/elastic/elasticsearch/pull/100\nkeep.yaml\n",
			TestContext.Current.CancellationToken);

		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			Config = configPath,
			Profile = "release",
			ProfileArgument = listFile
		};

		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("not a mix"));
	}

	[Fact]
	public async Task Bundle_WithFiles_RulesBundleStillApplies()
	{
		var feature = FileSystem.Path.Join(_changelogDir, "feature.yaml");
		var bugFix = FileSystem.Path.Join(_changelogDir, "bug-fix.yaml");
		await FileSystem.File.WriteAllTextAsync(feature, EntryKeep, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(bugFix, EntryBugFix, TestContext.Current.CancellationToken);

		var configContent = $"""
			bundle:
			  directory: {_changelogDir}
			  use_local_changelogs: true
			rules:
			  bundle:
			    exclude_types: bug-fix
			""";
		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var output = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		var input = new BundleChangelogsArguments
		{
			Config = configPath,
			Files = [feature, bugFix],
			Output = output
		};

		var result = await ServiceWithConfig.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		var bundle = await FileSystem.File.ReadAllTextAsync(output, TestContext.Current.CancellationToken);
		bundle.Should().Contain("name: feature.yaml");
		bundle.Should().NotContain("name: bug-fix.yaml");
	}

	[Fact]
	public async Task Bundle_WithFiles_ForcesLocalEvenWhenRepoResolves()
	{
		var keep = FileSystem.Path.Join(_changelogDir, "keep.yaml");
		await FileSystem.File.WriteAllTextAsync(keep, EntryKeep, TestContext.Current.CancellationToken);

		var configContent = $"""
			bundle:
			  directory: {_changelogDir}
			  repo: elasticsearch
			""";
		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
		var fetcher = new CdnChangelogEntryFetcher(LoggerFactory, handler, sleep: (_, _) => Task.CompletedTask);
		var service = new ChangelogBundlingService(LoggerFactory, ConfigurationContext, FileSystem, null, fetcher);

		var output = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		var input = new BundleChangelogsArguments
		{
			Config = configPath,
			Files = [keep],
			Output = output,
			Resolve = true
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		handler.RequestedPaths.Should().BeEmpty("--files must force local sourcing");
		var bundle = await FileSystem.File.ReadAllTextAsync(output, TestContext.Current.CancellationToken);
		bundle.Should().Contain("Keep me");
	}

	[Fact]
	public async Task Bundle_WithForceLocal_SourcesLocalDespiteResolvableRepo()
	{
		var local = FileSystem.Path.Join(_changelogDir, "1-local.yaml");
		await FileSystem.File.WriteAllTextAsync(local, EntryKeep, TestContext.Current.CancellationToken);

		var configContent = $"""
			bundle:
			  directory: {_changelogDir}
			  repo: elasticsearch
			""";
		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
		var fetcher = new CdnChangelogEntryFetcher(LoggerFactory, handler, sleep: (_, _) => Task.CompletedTask);
		var service = new ChangelogBundlingService(LoggerFactory, ConfigurationContext, FileSystem, null, fetcher);

		var output = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		var input = new BundleChangelogsArguments
		{
			Config = configPath,
			All = true,
			ForceLocal = true,
			Output = output,
			Resolve = true
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		handler.RequestedPaths.Should().BeEmpty("--force-local must not reach the CDN");
		var bundle = await FileSystem.File.ReadAllTextAsync(output, TestContext.Current.CancellationToken);
		bundle.Should().Contain("name: 1-local.yaml");
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
