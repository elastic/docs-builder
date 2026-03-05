// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Links.CrossLinks;
using FluentAssertions;

namespace Elastic.Markdown.Tests.CrossLinks;

/// <summary>Mirrors the path extraction logic in CodexBuildService.CollectRedirects.</summary>
internal static class RedirectPathExtractor
{
	public static string GetPath(Uri? uri) =>
		uri is null
			? string.Empty
			: uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString;
}

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

	[Fact]
	public void SameRepoRedirect_CurrentRepoInCodexSet_ProducesCodexPath()
	{
		var codexRepos = new HashSet<string> { "ai-guild" }.ToFrozenSet();
		var resolver = new CodexAwareUriResolver(codexRepos, useRelativePaths: true);
		var crossLinkUri = new Uri("ai-guild://best-practices/tools", UriKind.Absolute);
		var targetPath = CrossLinkResolver.ToTargetUrlPath("best-practices/tools");

		var result = resolver.Resolve(crossLinkUri, targetPath);

		result.IsAbsoluteUri.Should().BeFalse();
		result.ToString().Should().Be("/r/ai-guild/best-practices/tools");
	}

	[Fact]
	public void SameRepoRedirect_WithIndexNormalization_StripsTrailingIndex()
	{
		var codexRepos = new HashSet<string> { "ai-guild" }.ToFrozenSet();
		var resolver = new CodexAwareUriResolver(codexRepos, useRelativePaths: true);
		var crossLinkUri = new Uri("ai-guild://best-practices/tools/index.md", UriKind.Absolute);
		var targetPath = CrossLinkResolver.ToTargetUrlPath("best-practices/tools/index.md");

		var result = resolver.Resolve(crossLinkUri, targetPath);

		result.IsAbsoluteUri.Should().BeFalse();
		result.ToString().Should().Be("/r/ai-guild/best-practices/tools");
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

public class CodexRedirectPathExtractionTests
{
	[Fact]
	public void RelativeUri_FromCodexAwareResolver_ExtractsPathCorrectly()
	{
		var codexRepos = new HashSet<string> { "ai-guild" }.ToFrozenSet();
		var resolver = new CodexAwareUriResolver(codexRepos, useRelativePaths: true);
		var uri = resolver.Resolve(new Uri("ai-guild://tools", UriKind.Absolute), "tools");

		var path = RedirectPathExtractor.GetPath(uri);

		path.Should().Be("/r/ai-guild/tools");
	}

	[Fact]
	public void AbsoluteUri_FromCodexAwareResolver_ExtractsPathCorrectly()
	{
		var codexRepos = new HashSet<string> { "ai-guild" }.ToFrozenSet();
		var resolver = new CodexAwareUriResolver(codexRepos, useRelativePaths: false);
		var uri = resolver.Resolve(new Uri("ai-guild://tools", UriKind.Absolute), "tools");

		var path = RedirectPathExtractor.GetPath(uri);

		path.Should().Be("/r/ai-guild/tools");
	}

	[Fact]
	public void AbsoluteUri_FromIsolatedBuildResolver_ExtractsPathCorrectly()
	{
		var resolver = new IsolatedBuildEnvironmentUriResolver();
		var uri = resolver.Resolve(new Uri("docs-content://get-started", UriKind.Absolute), "get-started");

		var path = RedirectPathExtractor.GetPath(uri);

		path.Should().Be("/elastic/docs-content/tree/main/get-started");
	}

	[Fact]
	public void NullUri_ReturnsEmptyString()
	{
		var path = RedirectPathExtractor.GetPath(null);

		path.Should().BeEmpty();
	}
}

public class CodexCrossRepoRedirectTests
{
	[Fact]
	public void CrossRepoRedirect_TargetInCodexRepo_ResolvesToCodexPath()
	{
		var resolver = new Elastic.Markdown.Tests.TestCodexCrossLinkResolver(useRelativePaths: true);
		var crossRepoUri = new Uri("kibana://get-started/index.md", UriKind.Absolute);

		var success = resolver.TryResolve(_ => { }, crossRepoUri, out var resolvedUri);

		success.Should().BeTrue();
		resolvedUri.Should().NotBeNull();
		resolvedUri!.IsAbsoluteUri.Should().BeFalse();
		resolvedUri.ToString().Should().Be("/r/kibana/get-started");
	}
}
