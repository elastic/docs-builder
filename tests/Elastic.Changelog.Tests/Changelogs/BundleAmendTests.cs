// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration.ReleaseNotes;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs;

public class BundleAmendTests : ChangelogTestBase
{
	private ChangelogBundleAmendService Service { get; }
	private ChangelogBundlingService BundleService { get; }
	private readonly string _changelogDir;

	public BundleAmendTests(ITestOutputHelper output) : base(output)
	{
		Service = new(LoggerFactory, FileSystem);
		BundleService = new(LoggerFactory, null, FileSystem);
		_changelogDir = CreateChangelogDir();
	}

	private string CreateChangelogDir()
	{
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);
		return changelogDir;
	}

	/// <summary>
	/// Creates a resolved bundle file (entries have Title/Type inlined) and returns the bundle path.
	/// </summary>
	private async Task<string> CreateResolvedBundle(CancellationToken ct)
	{
		// language=yaml
		var changelog =
			"""
			title: Existing feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "100"
			""";

		var changelogFile = FileSystem.Path.Combine(_changelogDir, "1755268130-existing.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog, ct);

		var bundlePath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Resolve = true,
			Output = bundlePath
		};

		var result = await BundleService.BundleChangelogs(Collector, input, ct);
		result.Should().BeTrue("bundle creation should succeed");

		// Verify it's actually resolved
		var bundleContent = await FileSystem.File.ReadAllTextAsync(bundlePath, ct);
		bundleContent.Should().Contain("title: Existing feature");

		return bundlePath;
	}

	/// <summary>
	/// Creates an unresolved bundle file (entries only have File references) and returns the bundle path.
	/// </summary>
	private async Task<string> CreateUnresolvedBundle(CancellationToken ct)
	{
		// language=yaml
		var changelog =
			"""
			title: Existing feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "100"
			""";

		var changelogFile = FileSystem.Path.Combine(_changelogDir, "1755268130-existing.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog, ct);

		var bundlePath = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Resolve = false,
			Output = bundlePath
		};

		var result = await BundleService.BundleChangelogs(Collector, input, ct);
		result.Should().BeTrue("bundle creation should succeed");

		// Verify it's actually unresolved
		var bundleContent = await FileSystem.File.ReadAllTextAsync(bundlePath, ct);
		bundleContent.Should().NotContain("title: Existing feature");

		return bundlePath;
	}

	/// <summary>
	/// Creates a new changelog file to be added via amend, in a separate directory.
	/// Returns the file path.
	/// </summary>
	private async Task<string> CreateNewChangelogFile(CancellationToken ct)
	{
		var newDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(newDir);

		// language=yaml
		var newChangelog =
			"""
			title: New amended feature
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "200"
			description: A new enhancement added via amend
			""";

		var newFile = FileSystem.Path.Combine(newDir, "1755268200-new-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(newFile, newChangelog, ct);
		return newFile;
	}

	[Fact]
	public async Task AmendBundle_WhenOriginalIsResolved_InfersResolveTrue()
	{
		var ct = TestContext.Current.CancellationToken;
		var bundlePath = await CreateResolvedBundle(ct);
		var newFile = await CreateNewChangelogFile(ct);

		// Reset collector for the amend operation
		var amendCollector = new TestDiagnosticsCollector(Output);

		var input = new AmendBundleArguments
		{
			BundlePath = bundlePath,
			AddFiles = [newFile],
			Resolve = null // Should infer from original bundle
		};

		var result = await Service.AmendBundle(amendCollector, input, ct);

		result.Should().BeTrue();
		amendCollector.Errors.Should().Be(0);

		// Find the amend file
		var amendFiles = ChangelogBundleAmendService.DiscoverAmendFiles(FileSystem, bundlePath);
		amendFiles.Should().HaveCount(1);

		var amendContent = await FileSystem.File.ReadAllTextAsync(amendFiles[0], ct);

		// Amend file should contain resolved data (Title/Type inlined)
		amendContent.Should().Contain("title: New amended feature");
		amendContent.Should().Contain("type: enhancement");
		amendContent.Should().Contain("description: A new enhancement added via amend");
	}

	[Fact]
	public async Task AmendBundle_WhenOriginalIsUnresolved_InfersResolveFalse()
	{
		var ct = TestContext.Current.CancellationToken;
		var bundlePath = await CreateUnresolvedBundle(ct);
		var newFile = await CreateNewChangelogFile(ct);

		var amendCollector = new TestDiagnosticsCollector(Output);

		var input = new AmendBundleArguments
		{
			BundlePath = bundlePath,
			AddFiles = [newFile],
			Resolve = null // Should infer from original bundle
		};

		var result = await Service.AmendBundle(amendCollector, input, ct);

		result.Should().BeTrue();
		amendCollector.Errors.Should().Be(0);

		var amendFiles = ChangelogBundleAmendService.DiscoverAmendFiles(FileSystem, bundlePath);
		amendFiles.Should().HaveCount(1);

		var amendContent = await FileSystem.File.ReadAllTextAsync(amendFiles[0], ct);

		// Amend file should only contain file references, not resolved data
		amendContent.Should().Contain("file:");
		amendContent.Should().Contain("name: 1755268200-new-feature.yaml");
		amendContent.Should().Contain("checksum:");
		amendContent.Should().NotContain("title: New amended feature");
		amendContent.Should().NotContain("type: enhancement");
	}

	[Fact]
	public async Task AmendBundle_WithExplicitResolveTrue_OverridesUnresolvedBundle()
	{
		var ct = TestContext.Current.CancellationToken;
		var bundlePath = await CreateUnresolvedBundle(ct);
		var newFile = await CreateNewChangelogFile(ct);

		var amendCollector = new TestDiagnosticsCollector(Output);

		var input = new AmendBundleArguments
		{
			BundlePath = bundlePath,
			AddFiles = [newFile],
			Resolve = true // Explicit override
		};

		var result = await Service.AmendBundle(amendCollector, input, ct);

		result.Should().BeTrue();
		amendCollector.Errors.Should().Be(0);

		var amendFiles = ChangelogBundleAmendService.DiscoverAmendFiles(FileSystem, bundlePath);
		amendFiles.Should().HaveCount(1);

		var amendContent = await FileSystem.File.ReadAllTextAsync(amendFiles[0], ct);

		// Should be resolved despite original bundle being unresolved
		amendContent.Should().Contain("title: New amended feature");
		amendContent.Should().Contain("type: enhancement");
		amendContent.Should().Contain("description: A new enhancement added via amend");
	}

	[Fact]
	public async Task AmendBundle_WithExplicitResolveFalse_OverridesResolvedBundle()
	{
		var ct = TestContext.Current.CancellationToken;
		var bundlePath = await CreateResolvedBundle(ct);
		var newFile = await CreateNewChangelogFile(ct);

		var amendCollector = new TestDiagnosticsCollector(Output);

		var input = new AmendBundleArguments
		{
			BundlePath = bundlePath,
			AddFiles = [newFile],
			Resolve = false // Explicit override
		};

		var result = await Service.AmendBundle(amendCollector, input, ct);

		result.Should().BeTrue();
		amendCollector.Errors.Should().Be(0);

		var amendFiles = ChangelogBundleAmendService.DiscoverAmendFiles(FileSystem, bundlePath);
		amendFiles.Should().HaveCount(1);

		var amendContent = await FileSystem.File.ReadAllTextAsync(amendFiles[0], ct);

		// Should be unresolved despite original bundle being resolved
		amendContent.Should().Contain("file:");
		amendContent.Should().Contain("name: 1755268200-new-feature.yaml");
		amendContent.Should().NotContain("title: New amended feature");
		amendContent.Should().NotContain("type: enhancement");
	}
}
