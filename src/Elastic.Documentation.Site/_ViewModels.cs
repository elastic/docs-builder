// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;

namespace Elastic.Documentation.Site;

public static class GlobalSections
{
	public const string Head = "head";
	public const string Footer = "footer";
}

/// <summary>Configuration injected into the frontend for build-type-specific behavior (OTEL, HTMX).</summary>
public record FrontendConfig(string BuildType, string ServiceName, bool TelemetryEnabled, string RootPath);

public record GlobalLayoutViewModel
{
	public required string DocsBuilderVersion { get; init; }
	public required string DocSetName { get; init; }
	public string Title { get; set; } = "Elastic Documentation";
	public required string Description { get; init; }

	public required INavigationItem CurrentNavigationItem { get; init; }
	public required INavigationItem? Previous { get; init; }
	public required INavigationItem? Next { get; init; }

	public required string NavigationHtml { get; init; }
	public required string? UrlPathPrefix { get; init; }
	public required IHtmxAttributeProvider Htmx { get; init; }
	public required Uri? CanonicalBaseUrl { get; init; }

	// Header properties for isolated mode
	public string? HeaderTitle { get; init; }
	public string? HeaderVersion { get; init; }
	public string? GitBranch { get; init; }
	public string? GitCommitShort { get; init; }
	public string? GitRepository { get; init; }
	public string? GitHubDocsUrl { get; init; }
	/// <summary>Full ref from GitHub Actions (e.g. refs/pull/123/merge). Set when built in a pull request workflow.</summary>
	public string? GitHubRef { get; init; }
	public string? CanonicalUrl => CanonicalBaseUrl is not null ?
		new Uri(CanonicalBaseUrl, CurrentNavigationItem.Url).ToString().TrimEnd('/') : null;

	public required FeatureFlags Features { get; init; }
	// TODO move to @inject
	public required GoogleTagManagerConfiguration GoogleTagManager { get; init; }
	public required OptimizelyConfiguration Optimizely { get; init; }
	public required bool AllowIndexing { get; init; }
	public required StaticFileContentHashProvider StaticFileContentHashProvider { get; init; }

	public BuildType BuildType { get; init; } = BuildType.Isolated;

	public bool RenderHamburgerIcon { get; init; } = true;

	public FrontendConfig FrontendConfig =>
		BuildType switch
		{
			BuildType.Assembler => new FrontendConfig("assembler", "docs-frontend", true, "/docs"),
			BuildType.Codex => new FrontendConfig("codex", "codex-frontend", true, ""),
			_ => new FrontendConfig("isolated", "docs-frontend", false, ""),
		};

	public string FrontendConfigJson => ToJson(FrontendConfig);

	private static string ToJson(FrontendConfig c) =>
		$$"""
		{"buildType":"{{c.BuildType}}","serviceName":"{{c.ServiceName}}","telemetryEnabled":{{(c.TelemetryEnabled ? "true" : "false")}},"rootPath":"{{Escape(c.RootPath)}}"}
		""";

	private static string Escape(string? s) =>
		(s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

	public string Static(string path)
	{
		var staticPath = $"_static/{path.TrimStart('/')}";
		var contentHash = StaticFileContentHashProvider.GetContentHash(path.TrimStart('/'));

		// For codex builds, static assets are in the root, not in each documentation set's directory
		// Extract the root path by removing the /r/repoName part from the URL path prefix
		var staticPrefix = GetStaticPathPrefix();

		var fullPath = string.IsNullOrEmpty(staticPrefix)
			? $"/{staticPath}"
			: $"{staticPrefix}/{staticPath}";

		return string.IsNullOrEmpty(contentHash)
			? fullPath
			: $"{fullPath}?v={contentHash}";
	}

	private string GetStaticPathPrefix()
	{
		if (BuildType != BuildType.Codex)
			return UrlPathPrefix ?? string.Empty;
		// Extract site prefix from URL path (e.g., /internal-docs/r/repoName -> /internal-docs)
		if (UrlPathPrefix?.Contains("/r/", StringComparison.Ordinal) != true)
			return string.Empty;
		var rIndex = UrlPathPrefix.IndexOf("/r/", StringComparison.Ordinal);
		return rIndex > 0 ? UrlPathPrefix[..rIndex] : string.Empty;
	}

	public string Link(string path)
	{
		path = path.AsSpan().Trim('/').ToString();
		return $"{UrlPathPrefix}/{path}";
	}

	/// <summary>Link to the site root. For codex builds, returns the codex root (not the doc set root).</summary>
	public string SiteLink(string path)
	{
		path = path.AsSpan().Trim('/').ToString();
		var prefix = GetStaticPathPrefix();
		return string.IsNullOrEmpty(prefix) ? $"/{path}" : $"{prefix}/{path}";
	}
}
