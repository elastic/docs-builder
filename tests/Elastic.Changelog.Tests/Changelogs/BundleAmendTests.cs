// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;

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
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);
		return changelogDir;
	}

	/// <summary>
	/// Creates a bundle file (entries have Title/Type inlined with file provenance) and returns the bundle path.
	/// </summary>
	private async Task<string> CreateBundle(CancellationToken ct)
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

		var changelogFile = FileSystem.Path.Join(_changelogDir, "1755268130-existing.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog, ct);

		var bundlePath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		var input = new BundleChangelogsArguments
		{
			Directory = _changelogDir,
			All = true,
			Output = bundlePath
		};

		var result = await BundleService.BundleChangelogs(Collector, input, ct);
		result.Should().BeTrue("bundle creation should succeed");

		// Bundles are always resolved: entry content is inlined
		var bundleContent = await FileSystem.File.ReadAllTextAsync(bundlePath, ct);
		bundleContent.Should().Contain("title: Existing feature");

		return bundlePath;
	}

	/// <summary>
	/// Creates a new changelog file to be added via amend, in a separate directory.
	/// Returns the file path.
	/// </summary>
	private async Task<string> CreateNewChangelogFile(CancellationToken ct)
	{
		var newDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
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

		var newFile = FileSystem.Path.Join(newDir, "1755268200-new-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(newFile, newChangelog, ct);
		return newFile;
	}

	[Fact]
	public async Task AmendBundle_AddFile_WritesResolvedEntryWithProvenance()
	{
		var ct = TestContext.Current.CancellationToken;
		var bundlePath = await CreateBundle(ct);
		var newFile = await CreateNewChangelogFile(ct);

		// Reset collector for the amend operation
		var amendCollector = new TestDiagnosticsCollector(Output);

		var input = new AmendBundleArguments
		{
			BundlePath = bundlePath,
			AddFiles = [newFile]
		};

		var result = await Service.AmendBundle(amendCollector, input, ct);

		result.Should().BeTrue();
		amendCollector.Errors.Should().Be(0);

		// Find the amend file
		var amendFiles = ChangelogBundleAmendService.DiscoverAmendFiles(FileSystem, bundlePath);
		amendFiles.Should().HaveCount(1);

		var amendContent = await FileSystem.File.ReadAllTextAsync(amendFiles[0], ct);

		// Amend file entries are always resolved (Title/Type inlined) with file provenance kept
		amendContent.Should().Contain("title: New amended feature");
		amendContent.Should().Contain("type: enhancement");
		amendContent.Should().Contain("description: A new enhancement added via amend");
		amendContent.Should().Contain("name: 1755268200-new-feature.yaml");
		amendContent.Should().Contain("checksum:");
	}

	[Fact]
	public async Task AmendBundle_RemoveFromParent_CreatesExcludeEntries()
	{
		var ct = TestContext.Current.CancellationToken;
		var bundlePath = await CreateBundle(ct);

		var changelogFile = FileSystem.Path.Join(_changelogDir, "1755268130-existing.yaml");
		var amendCollector = new TestDiagnosticsCollector(Output);

		var input = new AmendBundleArguments
		{
			BundlePath = bundlePath,
			RemoveFiles = [changelogFile]
		};

		var result = await Service.AmendBundle(amendCollector, input, ct);

		result.Should().BeTrue();
		amendCollector.Errors.Should().Be(0);

		var amendFiles = ChangelogBundleAmendService.DiscoverAmendFiles(FileSystem, bundlePath);
		amendFiles.Should().HaveCount(1);

		var amendContent = await FileSystem.File.ReadAllTextAsync(amendFiles[0], ct);
		amendContent.Should().Contain("exclude-entries:");
		amendContent.Should().Contain("name: 1755268130-existing.yaml");
		// The parent's products are copied first; the exclusions follow.
		amendContent.TrimStart().Should().StartWith("products:");
	}

	[Fact]
	public async Task AmendBundle_RemoveAfterAdd_ExcludesAmendedEntry()
	{
		var ct = TestContext.Current.CancellationToken;
		var bundlePath = await CreateBundle(ct);
		var newFile = await CreateNewChangelogFile(ct);

		var addCollector = new TestDiagnosticsCollector(Output);
		var addInput = new AmendBundleArguments
		{
			BundlePath = bundlePath,
			AddFiles = [newFile]
		};
		(await Service.AmendBundle(addCollector, addInput, ct)).Should().BeTrue();

		var removeCollector = new TestDiagnosticsCollector(Output);
		var removeInput = new AmendBundleArguments
		{
			BundlePath = bundlePath,
			RemoveFiles = [newFile]
		};

		var result = await Service.AmendBundle(removeCollector, removeInput, ct);

		result.Should().BeTrue();
		removeCollector.Errors.Should().Be(0);

		var amendFiles = ChangelogBundleAmendService.DiscoverAmendFiles(FileSystem, bundlePath);
		amendFiles.Should().HaveCount(2);

		var removeAmendContent = await FileSystem.File.ReadAllTextAsync(amendFiles[1], ct);
		removeAmendContent.Should().Contain("exclude-entries:");
		removeAmendContent.Should().Contain("name: 1755268200-new-feature.yaml");
	}

	[Fact]
	public async Task AmendBundle_RemoveWithChecksumMismatch_WithoutForce_Fails()
	{
		var ct = TestContext.Current.CancellationToken;
		var bundlePath = await CreateBundle(ct);
		var changelogFile = FileSystem.Path.Join(_changelogDir, "1755268130-existing.yaml");

		await FileSystem.File.WriteAllTextAsync(
			changelogFile,
			"""
			title: Changed title
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "100"
			""",
			ct);

		var amendCollector = new TestDiagnosticsCollector(Output);
		var input = new AmendBundleArguments
		{
			BundlePath = bundlePath,
			RemoveFiles = [changelogFile]
		};

		var result = await Service.AmendBundle(amendCollector, input, ct);

		result.Should().BeFalse();
		amendCollector.Diagnostics.Should().ContainSingle(d =>
			d.Message.Contains("different checksum"));
	}

	[Fact]
	public async Task AmendBundle_RemoveAndAdd_InSingleAmendFile()
	{
		var ct = TestContext.Current.CancellationToken;
		var bundlePath = await CreateBundle(ct);
		var removeFile = FileSystem.Path.Join(_changelogDir, "1755268130-existing.yaml");
		var addFile = await CreateNewChangelogFile(ct);

		var amendCollector = new TestDiagnosticsCollector(Output);
		var input = new AmendBundleArguments
		{
			BundlePath = bundlePath,
			RemoveFiles = [removeFile],
			AddFiles = [addFile]
		};

		var result = await Service.AmendBundle(amendCollector, input, ct);

		result.Should().BeTrue();
		amendCollector.Errors.Should().Be(0);

		var amendFiles = ChangelogBundleAmendService.DiscoverAmendFiles(FileSystem, bundlePath);
		amendFiles.Should().HaveCount(1);

		var amendContent = await FileSystem.File.ReadAllTextAsync(amendFiles[0], ct);
		amendContent.Should().Contain("exclude-entries:");
		amendContent.Should().Contain("name: 1755268130-existing.yaml");
		amendContent.Should().Contain("entries:");
		amendContent.Should().Contain("name: 1755268200-new-feature.yaml");
	}

	/// <summary>
	/// Writes a resolved parent bundle with a complete products block (target, lifecycle, repo, owner)
	/// plus its referenced changelog entry, and returns the bundle path.
	/// </summary>
	private async Task<string> CreateBundleWithFullProducts(CancellationToken ct)
	{
		// language=yaml
		var changelog =
			"""
			title: Existing feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			- "100"
			""";

		var changelogFile = FileSystem.Path.Join(_changelogDir, "1755268130-existing.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog, ct);
		var checksum = ComputeSha1(changelog);

		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);
		var bundlePath = FileSystem.Path.Join(bundleDir, "elasticsearch-9.3.0.yaml");
		// language=yaml
		await FileSystem.File.WriteAllTextAsync(bundlePath, $"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			  lifecycle: ga
			  repo: elasticsearch
			  owner: elastic
			entries:
			- file:
			    name: 1755268130-existing.yaml
			    checksum: {checksum}
			  type: feature
			  title: Existing feature
			  prs:
			  - "100"
			""", ct);

		return bundlePath;
	}

	[Fact]
	public async Task AmendBundle_Add_CopiesParentProductsIntoAmend()
	{
		var ct = TestContext.Current.CancellationToken;
		var bundlePath = await CreateBundleWithFullProducts(ct);
		var newFile = await CreateNewChangelogFile(ct);

		var amendCollector = new TestDiagnosticsCollector(Output);
		var input = new AmendBundleArguments
		{
			BundlePath = bundlePath,
			AddFiles = [newFile]
		};

		var result = await Service.AmendBundle(amendCollector, input, ct);

		result.Should().BeTrue();
		amendCollector.Errors.Should().Be(0);

		var amendFiles = ChangelogBundleAmendService.DiscoverAmendFiles(FileSystem, bundlePath);
		amendFiles.Should().HaveCount(1);

		var amendContent = await FileSystem.File.ReadAllTextAsync(amendFiles[0], ct);
		var amend = ReleaseNotesSerialization.DeserializeBundle(amendContent);

		// The amend must be self-contained: complete parent products, including target, repo, and owner.
		amend.Products.Should().ContainSingle();
		amend.Products[0].Should().BeEquivalentTo(new BundledProduct
		{
			ProductId = "elasticsearch",
			Target = "9.3.0",
			Lifecycle = Lifecycle.Ga,
			Repo = "elasticsearch",
			Owner = "elastic"
		});
	}

	[Fact]
	public async Task AmendBundle_Remove_CopiesParentProductsIntoAmend()
	{
		var ct = TestContext.Current.CancellationToken;
		var bundlePath = await CreateBundleWithFullProducts(ct);
		var changelogFile = FileSystem.Path.Join(_changelogDir, "1755268130-existing.yaml");

		var amendCollector = new TestDiagnosticsCollector(Output);
		var input = new AmendBundleArguments
		{
			BundlePath = bundlePath,
			RemoveFiles = [changelogFile]
		};

		var result = await Service.AmendBundle(amendCollector, input, ct);

		result.Should().BeTrue();
		amendCollector.Errors.Should().Be(0);

		var amendFiles = ChangelogBundleAmendService.DiscoverAmendFiles(FileSystem, bundlePath);
		amendFiles.Should().HaveCount(1);

		var amend = ReleaseNotesSerialization.DeserializeBundle(await FileSystem.File.ReadAllTextAsync(amendFiles[0], ct));
		amend.ExcludeEntries.Should().ContainSingle();
		amend.Products.Should().ContainSingle();
		amend.Products[0].Target.Should().Be("9.3.0");
		amend.Products[0].Repo.Should().Be("elasticsearch");
		amend.Products[0].Owner.Should().Be("elastic");
	}

	[Fact]
	public async Task AmendBundle_CorruptExistingAmend_FailsWithoutWritingNewAmend()
	{
		var ct = TestContext.Current.CancellationToken;
		var bundlePath = await CreateBundle(ct);
		var changelogFile = FileSystem.Path.Join(_changelogDir, "1755268130-existing.yaml");

		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.ChangeExtension(bundlePath, ".amend-1.yaml"),
			"exclude-entries:\n  - file: [invalid yaml",
			ct);

		var amendCollector = new TestDiagnosticsCollector(Output);
		var input = new AmendBundleArguments
		{
			BundlePath = bundlePath,
			RemoveFiles = [changelogFile]
		};

		var result = await Service.AmendBundle(amendCollector, input, ct);

		result.Should().BeFalse();
		amendCollector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("Failed to deserialize amend file"));

		var amendFiles = ChangelogBundleAmendService.DiscoverAmendFiles(FileSystem, bundlePath);
		amendFiles.Should().HaveCount(1, "corrupt amend should not produce a second amend file");
	}
}
