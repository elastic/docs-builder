// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Links.CrossLinks;
using FluentAssertions;

namespace Elastic.Markdown.Tests.CrossLinks;

public class CodexAwareUriResolverTests
{
	private static readonly FrozenSet<string> CodexRepos =
		new HashSet<string> { "observability-robots", "docs-eng-team" }.ToFrozenSet();

	[Fact]
	public void CodexRepo_RelativeMode_ProducesPathOnly()
	{
		var resolver = new CodexAwareUriResolver(CodexRepos, useRelativePaths: true);
		var uri = new Uri("observability-robots://some-page.md", UriKind.Absolute);

		var result = resolver.Resolve(uri, "some-page");

		result.IsAbsoluteUri.Should().BeFalse();
		result.ToString().Should().Be("/r/observability-robots/some-page");
	}

	[Fact]
	public void CodexRepo_AbsoluteMode_ProducesFullUrl()
	{
		var resolver = new CodexAwareUriResolver(CodexRepos, useRelativePaths: false);
		var uri = new Uri("observability-robots://some-page.md", UriKind.Absolute);

		var result = resolver.Resolve(uri, "some-page");

		result.IsAbsoluteUri.Should().BeTrue();
		result.ToString().Should().Be("https://codex.elastic.dev/r/observability-robots/some-page");
	}

	[Fact]
	public void CodexRepo_EmptyPath_RelativeMode()
	{
		var resolver = new CodexAwareUriResolver(CodexRepos, useRelativePaths: true);
		var uri = new Uri("observability-robots://index.md", UriKind.Absolute);

		var result = resolver.Resolve(uri, "");

		result.IsAbsoluteUri.Should().BeFalse();
		result.ToString().Should().Be("/r/observability-robots/");
	}

	[Fact]
	public void CodexRepo_EmptyPath_AbsoluteMode()
	{
		var resolver = new CodexAwareUriResolver(CodexRepos, useRelativePaths: false);
		var uri = new Uri("observability-robots://index.md", UriKind.Absolute);

		var result = resolver.Resolve(uri, "");

		result.IsAbsoluteUri.Should().BeTrue();
		result.ToString().Should().Be("https://codex.elastic.dev/r/observability-robots/");
	}

	[Fact]
	public void NonCodexRepo_FallsBackToPublicResolver()
	{
		var resolver = new CodexAwareUriResolver(CodexRepos, useRelativePaths: true);
		var uri = new Uri("docs-content://get-started/index.md", UriKind.Absolute);

		var result = resolver.Resolve(uri, "get-started");

		result.IsAbsoluteUri.Should().BeTrue();
		result.ToString().Should().Contain("docs-v3-preview.elastic.dev");
		result.ToString().Should().Contain("docs-content");
	}

	[Fact]
	public void DefaultMode_IsAbsolute()
	{
		var resolver = new CodexAwareUriResolver(CodexRepos);
		var uri = new Uri("observability-robots://page.md", UriKind.Absolute);

		var result = resolver.Resolve(uri, "page");

		result.IsAbsoluteUri.Should().BeTrue();
		result.ToString().Should().Be("https://codex.elastic.dev/r/observability-robots/page");
	}
}

public class IsolatedBuildEnvironmentUriResolverTests
{
	[Fact]
	public void ProducesAbsoluteUrl()
	{
		var resolver = new IsolatedBuildEnvironmentUriResolver();
		var uri = new Uri("docs-content://get-started/index.md", UriKind.Absolute);

		var result = resolver.Resolve(uri, "get-started");

		result.IsAbsoluteUri.Should().BeTrue();
		result.ToString().Should().Be("https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/get-started");
	}

	[Fact]
	public void CloudRepo_UsesMasterBranch()
	{
		var resolver = new IsolatedBuildEnvironmentUriResolver();
		var uri = new Uri("cloud://page.md", UriKind.Absolute);

		var result = resolver.Resolve(uri, "page");

		result.ToString().Should().Contain("/tree/master/");
	}

	[Fact]
	public void NonCloudRepo_UsesMainBranch()
	{
		var resolver = new IsolatedBuildEnvironmentUriResolver();
		var uri = new Uri("elasticsearch://page.md", UriKind.Absolute);

		var result = resolver.Resolve(uri, "page");

		result.ToString().Should().Contain("/tree/main/");
	}
}
