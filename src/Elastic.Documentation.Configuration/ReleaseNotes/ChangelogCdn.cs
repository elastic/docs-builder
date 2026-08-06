// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// Single source of truth for the public changelog CDN base URL used by both the <c>changelog</c>
/// directive (<c>:cdn:</c> mode) and the <c>changelog bundle</c> command (CDN entry sourcing).
/// </summary>
public static class ChangelogCdn
{
	/// <summary>
	/// Environment variable that overrides the changelog CDN base URL (staging/local/testing).
	/// </summary>
	public const string BaseUrlEnvironmentVariable = "DOCS_BUILDER_CHANGELOG_CDN";

	/// <summary>
	/// Default public CDN base for changelog content (CloudFront in front of the public S3 bucket).
	/// Overridable via <see cref="BaseUrlEnvironmentVariable"/>.
	/// </summary>
	public const string DefaultBaseUrl = "https://d10xozp44eyz7q.cloudfront.net";

	/// <summary>
	/// Resolves the configured CDN base URI, honoring <see cref="BaseUrlEnvironmentVariable"/> and
	/// falling back to <see cref="DefaultBaseUrl"/>. Returns null when the configured value is not a
	/// valid absolute http(s) URL.
	/// </summary>
	public static Uri? ResolveBaseUri()
	{
		var configured = Environment.GetEnvironmentVariable(BaseUrlEnvironmentVariable);
		var raw = string.IsNullOrWhiteSpace(configured) ? DefaultBaseUrl : configured;
		return Uri.TryCreate(raw, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
			? uri
			: null;
	}
}
