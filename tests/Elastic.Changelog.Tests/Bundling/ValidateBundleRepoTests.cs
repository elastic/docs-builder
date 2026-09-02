// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests.Bundling;

public class ValidateBundleRepoTests(ITestOutputHelper output)
{
	private readonly TestDiagnosticsCollector _collector = new(output);
	private readonly MockFileSystem _fileSystem = new();

	[Fact]
	public void ValidateBundleRepo_UnsetBundleRepo_EmitsNothing()
	{
		BundleOutputNaming.ValidateBundleRepo(_collector, _fileSystem, null, null);

		_collector.Errors.Should().Be(0);
		_collector.Diagnostics.Should().BeEmpty();
	}

	[Fact]
	public void ValidateBundleRepo_EmptyBundleRepo_EmitsNothing()
	{
		BundleOutputNaming.ValidateBundleRepo(_collector, _fileSystem, null, string.Empty);

		_collector.Errors.Should().Be(0);
		_collector.Diagnostics.Should().BeEmpty();
	}

	[Fact]
	public void ValidateBundleRepo_SetButNoAuthoritativeSource_EmitsNothing()
	{
		// No GITHUB_REPOSITORY env var, no git remote — cannot validate, so silently skip.
		BundleOutputNaming.ValidateBundleRepo(_collector, _fileSystem, null, "docs-builder");

		_collector.Errors.Should().Be(0);
		_collector.Diagnostics.Should().BeEmpty();
	}

	[Fact]
	public void ValidateBundleRepo_MatchesGithubRepository_EmitsWarning()
	{
		Environment.SetEnvironmentVariable("GITHUB_REPOSITORY", "elastic/docs-builder");
		try
		{
			BundleOutputNaming.ValidateBundleRepo(_collector, _fileSystem, null, "docs-builder");

			_collector.Errors.Should().Be(0);
			var warnings = _collector.Diagnostics.Where(d => d.Severity == Severity.Warning).ToList();
			warnings.Should().HaveCount(1);
			warnings[0].Message.Should().Contain("redundant");
		}
		finally
		{
			Environment.SetEnvironmentVariable("GITHUB_REPOSITORY", null);
		}
	}

	[Fact]
	public void ValidateBundleRepo_DiffersFromGithubRepository_EmitsError()
	{
		Environment.SetEnvironmentVariable("GITHUB_REPOSITORY", "elastic/docs-builder");
		try
		{
			BundleOutputNaming.ValidateBundleRepo(_collector, _fileSystem, null, "kibana");

			_collector.Errors.Should().Be(1);
			_collector.Diagnostics.First(d => d.Severity == Severity.Error).Message.Should().Contain("docs-builder").And.Contain("kibana");
		}
		finally
		{
			Environment.SetEnvironmentVariable("GITHUB_REPOSITORY", null);
		}
	}
}
