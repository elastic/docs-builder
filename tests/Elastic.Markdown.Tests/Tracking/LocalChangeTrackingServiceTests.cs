// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Refactor.Tracking;
using Nullean.ScopedFileSystem;

namespace Elastic.Markdown.Tests.Tracking;

/// <summary>
/// End-to-end tests for <see cref="LocalChangeTrackingService.ValidateRedirects"/>, which
/// is what runs when CI executes <c>docs-builder diff validate</c>. The tests simulate
/// the CI path (GITHUB_ACTIONS=true) so the service reads changes from environment
/// variables rather than shelling out to git.
/// </summary>
[Collection(TrackingTestCollection.Name)]
public sealed class LocalChangeTrackingServiceTests : IDisposable
{
	private static readonly string[] EnvVarNames =
	[
		"GITHUB_ACTIONS",
		"ADDED_FILES",
		"MODIFIED_FILES",
		"DELETED_FILES",
		"RENAMED_FILES"
	];

	private readonly ITestOutputHelper _output;

	public LocalChangeTrackingServiceTests(ITestOutputHelper output)
	{
		_output = output;
		ClearEnv();
	}

	public void Dispose() => ClearEnv();

	private static void ClearEnv()
	{
		foreach (var name in EnvVarNames)
			Environment.SetEnvironmentVariable(name, null);
	}

	[Fact]
	public async Task DocsetAtRepoRoot_DeletedMarkdownWithoutRedirect_EmitsError()
	{
		Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
		Environment.SetEnvironmentVariable("DELETED_FILES", "troubleshoot/deployments/serverless.md");

		var (service, collector, fs, source) = CreateService(docsetAtRoot: true, redirects: "");

		var ok = await service.ValidateRedirects(collector, source, fs);

		ok.Should().BeFalse();
		collector.Errors.Should().BeGreaterThan(0);
		collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("troubleshoot/deployments/serverless.md") &&
			d.Message.Contains("redirects.yml"));
	}

	[Fact]
	public async Task DocsetAtRepoRoot_DeletedMarkdownWithRedirect_Succeeds()
	{
		Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
		Environment.SetEnvironmentVariable("DELETED_FILES", "troubleshoot/deployments/serverless.md");

		var (service, collector, fs, source) = CreateService(
			docsetAtRoot: true,
			redirects: """
				redirects:
				  troubleshoot/deployments/serverless.md: troubleshoot/deployments.md
				""");

		var ok = await service.ValidateRedirects(collector, source, fs);

		ok.Should().BeTrue();
		collector.Errors.Should().Be(0);
	}

	[Fact]
	public async Task DocsetInSubfolder_DeletedMarkdownWithoutRedirect_EmitsError()
	{
		Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
		Environment.SetEnvironmentVariable("DELETED_FILES", "docs/reference/old-page.md");

		var (service, collector, fs, source) = CreateService(docsetAtRoot: false, redirects: "");

		var ok = await service.ValidateRedirects(collector, source, fs);

		ok.Should().BeFalse();
		collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("reference/old-page.md"));
	}

	[Fact]
	public async Task DocsetInSubfolder_ChangesOutsideDocset_AreIgnored()
	{
		Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
		Environment.SetEnvironmentVariable("DELETED_FILES", "other/foo.md");

		var (service, collector, fs, source) = CreateService(docsetAtRoot: false, redirects: "");

		var ok = await service.ValidateRedirects(collector, source, fs);

		ok.Should().BeTrue();
		collector.Errors.Should().Be(0);
	}

	[Fact]
	public async Task SnippetsDeletions_AreIgnored()
	{
		Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
		Environment.SetEnvironmentVariable("DELETED_FILES", "_snippets/shared.md");

		var (service, collector, fs, source) = CreateService(docsetAtRoot: true, redirects: "");

		var ok = await service.ValidateRedirects(collector, source, fs);

		ok.Should().BeTrue();
		collector.Errors.Should().Be(0);
	}

	[Fact]
	public async Task RenamedMarkdownWithoutRedirect_EmitsError()
	{
		Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
		Environment.SetEnvironmentVariable("RENAMED_FILES", "troubleshoot/old.md:troubleshoot/new.md");

		var (service, collector, fs, source) = CreateService(docsetAtRoot: true, redirects: "");

		var ok = await service.ValidateRedirects(collector, source, fs);

		ok.Should().BeFalse();
		collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("troubleshoot/old.md") &&
			d.Message.Contains("troubleshoot/new.md"));
	}

	[Fact]
	public async Task NoRedirectsFile_SkipsValidation()
	{
		Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
		Environment.SetEnvironmentVariable("DELETED_FILES", "troubleshoot/deployments/serverless.md");

		var (service, collector, fs, source) = CreateService(docsetAtRoot: true, redirects: null);

		var ok = await service.ValidateRedirects(collector, source, fs);

		ok.Should().BeTrue();
		collector.Errors.Should().Be(0);
	}

	private (LocalChangeTrackingService service, TestDiagnosticsCollector collector, ScopedFileSystem fs, string source)
		CreateService(bool docsetAtRoot, string? redirects)
	{
		var root = Paths.WorkingDirectoryRoot.FullName;
		var repoPath = Path.Combine(root, $"diff-validate-test-{Guid.NewGuid():N}");
		var docsetDir = docsetAtRoot ? repoPath : Path.Combine(repoPath, "docs");

		var fileSystem = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = root });
		fileSystem.AddDirectory(Path.Combine(repoPath, ".git"));
		fileSystem.AddFile(Path.Combine(docsetDir, "docset.yml"), new MockFileData("project: test\ntoc:\n- file: index.md\n"));
		fileSystem.AddFile(Path.Combine(docsetDir, "index.md"), new MockFileData("# Home"));
		if (redirects is not null)
		{
			// Empty string means "redirects.yml exists but has no entries". Use a valid YAML
			// stub so the parser does not flag the file as unparseable.
			var contents = string.IsNullOrWhiteSpace(redirects) ? "redirects: {}\n" : redirects;
			fileSystem.AddFile(Path.Combine(docsetDir, "redirects.yml"), new MockFileData(contents));
		}

		var scoped = FileSystemFactory.ScopeCurrentWorkingDirectory(fileSystem);
		var collector = new TestDiagnosticsCollector(_output);
		_ = collector.StartAsync(TestContext.Current.CancellationToken);

		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var service = new LocalChangeTrackingService(new TestLoggerFactory(_output), configurationContext);

		return (service, collector, scoped, docsetDir);
	}
}
