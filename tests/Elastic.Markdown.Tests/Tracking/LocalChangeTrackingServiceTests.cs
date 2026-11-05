// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Refactor.Tracking;
using Elastic.Documentation.Testing;
using Nullean.ScopedFileSystem;
using Nullean.Snapshots;

namespace Elastic.Markdown.Tests.Tracking;

/// <summary>
/// End-to-end tests for <see cref="LocalChangeTrackingService.ValidateRedirects"/>, which
/// is what runs when CI executes <c>docs-builder diff validate</c>. The tests simulate
/// the CI path (GITHUB_ACTIONS=true) so the service reads changes from environment
/// variables rather than shelling out to git.
/// </summary>
[NotInParallel("Tracking")]
public sealed class LocalChangeTrackingServiceTests
{
	private static readonly string[] EnvVarNames = ["GITHUB_ACTIONS", "ADDED_FILES", "MODIFIED_FILES", "DELETED_FILES", "RENAMED_FILES"];

	private readonly List<string> _tempDirectories = [];

	[Before(Test)]
	public void ClearEnv()
	{
		foreach (var name in EnvVarNames)
			Environment.SetEnvironmentVariable(name, null);
	}

	[After(Test)]
	public void Cleanup()
	{
		ClearEnv();
		foreach (var dir in _tempDirectories.Where(Directory.Exists))
			Directory.Delete(dir, recursive: true);
	}

	[Test]
	public async Task DocsetAtRepoRoot_DeletedMarkdownWithoutRedirect_EmitsError()
	{
		Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
		Environment.SetEnvironmentVariable("DELETED_FILES", "troubleshoot/deployments/serverless.md");

		var (service, collector, fs, source) = CreateService(docsetAtRoot: true, redirects: "");

		var ok = await service.ValidateRedirects(collector, source, fs);

		ok.Should().BeFalse();
		collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error {temp} Missing 'troubleshoot/deployments/serverless.md' in redirects.yml. 'troubleshoot/deployments/serverless.md' was deleted but it has no redirect targets. This will lead to broken links."
			);
	}

	[Test]
	public async Task DocsetAtRepoRoot_DeletedMarkdownWithRedirect_Succeeds()
	{
		Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
		Environment.SetEnvironmentVariable("DELETED_FILES", "troubleshoot/deployments/serverless.md");

		var (service, collector, fs, source) = CreateService(
			docsetAtRoot: true,
			redirects: """
				redirects:
				  troubleshoot/deployments/serverless.md: troubleshoot/deployments.md
				"""
		);

		var ok = await service.ValidateRedirects(collector, source, fs);

		ok.Should().BeTrue();
		collector.Errors.Should().Be(0);
	}

	[Test]
	public async Task DocsetInSubfolder_DeletedMarkdownWithoutRedirect_EmitsError()
	{
		Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
		Environment.SetEnvironmentVariable("DELETED_FILES", "docs/reference/old-page.md");

		var (service, collector, fs, source) = CreateService(docsetAtRoot: false, redirects: "");

		var ok = await service.ValidateRedirects(collector, source, fs);

		ok.Should().BeFalse();
		collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error {temp} Missing 'reference/old-page.md' in redirects.yml. 'docs/reference/old-page.md' was deleted but it has no redirect targets. This will lead to broken links."
			);
	}

	[Test]
	public async Task DocsetInSubfolder_ChangesOutsideDocset_AreIgnored()
	{
		Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
		Environment.SetEnvironmentVariable("DELETED_FILES", "other/foo.md");

		var (service, collector, fs, source) = CreateService(docsetAtRoot: false, redirects: "");

		var ok = await service.ValidateRedirects(collector, source, fs);

		ok.Should().BeTrue();
		collector.Errors.Should().Be(0);
	}

	[Test]
	public async Task SnippetsDeletions_AreIgnored()
	{
		Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
		Environment.SetEnvironmentVariable("DELETED_FILES", "_snippets/shared.md");

		var (service, collector, fs, source) = CreateService(docsetAtRoot: true, redirects: "");

		var ok = await service.ValidateRedirects(collector, source, fs);

		ok.Should().BeTrue();
		collector.Errors.Should().Be(0);
	}

	[Test]
	public async Task RenamedMarkdownWithoutRedirect_EmitsError()
	{
		Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
		Environment.SetEnvironmentVariable("RENAMED_FILES", "troubleshoot/old.md:troubleshoot/new.md");

		var (service, collector, fs, source) = CreateService(docsetAtRoot: true, redirects: "");

		var ok = await service.ValidateRedirects(collector, source, fs);

		ok.Should().BeFalse();
		collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error {temp} Missing 'troubleshoot/old.md' in redirects.yml. 'troubleshoot/old.md' was renamed to 'troubleshoot/new.md' but it has no redirect configuration set."
			);
	}

	[Test]
	public async Task NoRedirectsFile_SkipsValidation()
	{
		Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
		Environment.SetEnvironmentVariable("DELETED_FILES", "troubleshoot/deployments/serverless.md");

		var (service, collector, fs, source) = CreateService(docsetAtRoot: true, redirects: null);

		var ok = await service.ValidateRedirects(collector, source, fs);

		ok.Should().BeTrue();
		collector.Errors.Should().Be(0);
	}

	// LocalChangeTrackingService anchors on the physical file system (DocumentationFileSystem.Resolve),
	// so the repository fixture is written to a real temporary directory.
	private (LocalChangeTrackingService service, TestDiagnosticsCollector collector, ScopedFileSystem fs, string source) CreateService(
		bool docsetAtRoot,
		string? redirects
	)
	{
		var repoPath = Path.Combine(Path.GetTempPath(), $"diff-validate-test-{Guid.NewGuid():N}");
		_tempDirectories.Add(repoPath);
		var docsetDir = docsetAtRoot ? repoPath : Path.Combine(repoPath, "docs");
		_ = Directory.CreateDirectory(Path.Combine(repoPath, ".git"));
		_ = Directory.CreateDirectory(docsetDir);
		File.WriteAllText(Path.Combine(docsetDir, "docset.yml"), "project: test\ntoc:\n- file: index.md\n");
		File.WriteAllText(Path.Combine(docsetDir, "index.md"), "# Home");
		if (redirects is not null)
		{
			// Empty string means "redirects.yml exists but has no entries". Use a valid YAML
			// stub so the parser does not flag the file as unparseable.
			var contents = string.IsNullOrWhiteSpace(redirects) ? "redirects: {}\n" : redirects;
			File.WriteAllText(Path.Combine(docsetDir, "redirects.yml"), contents);
		}

		var scoped = DocumentationFileSystem.Resolve(docsetDir);
		var collector = new TestDiagnosticsCollector();
		_ = collector.StartAsync(TestContext.Current!.Execution.CancellationToken);

		var configurationContext = TestHelpers.CreateConfigurationContext(new MockFileSystem());
		var service = new LocalChangeTrackingService(new TestLoggerFactory(), configurationContext);

		return (service, collector, scoped, docsetDir);
	}
}
