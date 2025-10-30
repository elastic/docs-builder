// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Links.CrossLinks;

namespace Elastic.Documentation.Assembler.Links;

/// <summary>
/// Resolves cross-link URIs (e.g., elasticsearch://path/to/file.md) to absolute HTTP URLs
/// based on the navigation configuration's source → path_prefix mappings.
/// </summary>
public class PublishEnvironmentUriResolver : IUriEnvironmentResolver
{
	private readonly FrozenDictionary<Uri, NavigationTocMapping> _navigationMappings;
	private readonly Uri _baseUri;
	private readonly string? _pathPrefix;

	/// <summary>
	/// Creates a new resolver that maps cross-link URIs to absolute URLs.
	/// </summary>
	/// <param name="navigationMappings">Mappings from navigation.yml (toc sources to path prefixes)</param>
	/// <param name="environment">The publish environment containing base URI and optional path prefix</param>
	public PublishEnvironmentUriResolver(FrozenDictionary<Uri, NavigationTocMapping> navigationMappings, PublishEnvironment environment)
	{
		_navigationMappings = navigationMappings;
		_pathPrefix = environment.PathPrefix;

		if (!Uri.TryCreate(environment.Uri, UriKind.Absolute, out var uri))
			throw new Exception($"Could not parse uri {environment.Uri} in environment {environment}");

		_baseUri = uri;
	}

	/// <summary>
	/// Resolves a cross-link URI to an absolute HTTP URL using navigation mappings.
	/// </summary>
	/// <param name="crossLinkUri">The cross-link URI (e.g., elasticsearch://reference/query-dsl)</param>
	/// <param name="path">The relative file path within the repository (e.g., reference/query-dsl), already URL-formatted</param>
	/// <returns>The absolute HTTP URL based on navigation.yml mappings</returns>
	/// <example>
	/// Given navigation.yml has:
	///   - toc: elasticsearch://reference
	///     path_prefix: docs/elasticsearch/reference
	///
	/// Input: crossLinkUri = elasticsearch://reference/query-dsl, path = reference/query-dsl
	/// Output: https://www.elastic.co/docs/elasticsearch/reference/query-dsl
	/// </example>
	public Uri Resolve(Uri crossLinkUri, string path)
	{
		// The path parameter is the repository-relative path from links.json, converted to URL format
		// Example: elasticsearch://reference/query-dsl/bool-query.md → path = "reference/query-dsl/bool-query"

		// Find the navigation mapping for this source
		var mapping = FindBestMatchForSource(crossLinkUri);

		if (mapping != null)
		{
			// The navigation defines how this source maps to a URL path
			// Extract what part of 'path' is beyond the source prefix
			var sourcePrefix = $"{mapping.Source.Host}/{mapping.Source.AbsolutePath.TrimStart('/')}".Trim('/');
			var remainingPath = path;

			// If the path starts with the source prefix, get the remainder
			if (!string.IsNullOrEmpty(sourcePrefix) && path.StartsWith(sourcePrefix, StringComparison.Ordinal))
			{
				remainingPath = path.Length > sourcePrefix.Length
					? path.Substring(sourcePrefix.Length).TrimStart('/')
					: string.Empty;
			}

			// Build final path: path_prefix + remaining path
			var finalPath = string.IsNullOrEmpty(remainingPath)
				? mapping.SourcePathPrefix
				: $"{mapping.SourcePathPrefix}/{remainingPath}";

			// Apply environment prefix if present
			if (!string.IsNullOrEmpty(_pathPrefix))
				finalPath = $"{_pathPrefix}/{finalPath.TrimStart('/')}";

			return new Uri(_baseUri, finalPath);
		}

		// No mapping found - use path as-is with optional environment prefix
		var fallbackPath = !string.IsNullOrEmpty(_pathPrefix) ? $"{_pathPrefix}/{path.TrimStart('/')}" : path;
		return new Uri(_baseUri, fallbackPath);
	}

	/// <summary>
	/// Finds the best (longest) matching navigation mapping for a cross-link URI.
	/// Uses longest-prefix matching to handle nested sources.
	/// </summary>
	/// <example>
	/// If navigation has:
	///   - elasticsearch://reference → docs/elasticsearch/reference
	///   - elasticsearch://reference/query-dsl → docs/elasticsearch/reference/query-dsl
	///
	/// For "elasticsearch://reference/query-dsl/bool-query", we match the longer (more specific) mapping.
	/// </example>
	private NavigationTocMapping? FindBestMatchForSource(Uri crossLinkUri)
	{
		NavigationTocMapping? bestMatch = null;
		var bestMatchLength = -1;

		// Build the full source path from the cross-link URI
		var crossLinkSource = $"{crossLinkUri.Scheme}://{crossLinkUri.Host}/{crossLinkUri.AbsolutePath.TrimStart('/')}".TrimEnd('/');

		foreach (var mapping in _navigationMappings.Values)
		{
			// Build the mapping's source as a string for comparison
			var mappingSource = $"{mapping.Source.Scheme}://{mapping.Source.Host}/{mapping.Source.AbsolutePath.TrimStart('/')}".TrimEnd('/');

			// Check if the cross-link starts with this mapping's source
			if (crossLinkSource.StartsWith(mappingSource, StringComparison.Ordinal))
			{
				// Keep the longest (most specific) match
				if (mappingSource.Length > bestMatchLength)
				{
					bestMatch = mapping;
					bestMatchLength = mappingSource.Length;
				}
			}
		}

		return bestMatch;
	}

	/// <summary>
	/// Resolves a cross-link URI to all its sub-paths for validation purposes.
	/// Used by NavigationPrefixChecker to detect path collisions.
	/// </summary>
	/// <param name="crossLinkUri">The cross-link URI to resolve</param>
	/// <param name="path">The relative path within the repository</param>
	/// <returns>Array of URL path prefixes for collision detection</returns>
	public string[] ResolveToSubPaths(Uri crossLinkUri, string path)
	{
		// Find the navigation mapping
		var mapping = FindBestMatchForSource(crossLinkUri);

		if (mapping == null)
			return [];

		// Get the source prefix to calculate the relative path
		var sourcePrefix = $"{mapping.Source.Host}/{mapping.Source.AbsolutePath.TrimStart('/')}".Trim('/');
		var remainingPath = path;

		if (!string.IsNullOrEmpty(sourcePrefix) && path.StartsWith(sourcePrefix, StringComparison.Ordinal))
		{
			remainingPath = path.Length > sourcePrefix.Length
				? path.Substring(sourcePrefix.Length).TrimStart('/')
				: string.Empty;
		}

		// Build all sub-paths for this URL path
		// For example, "reference/query-dsl/bool-query" generates:
		// - "reference/"
		// - "reference/query-dsl/"
		// - "reference/query-dsl/bool-query/"
		var urlPath = MarkdownPathToUrlPath(remainingPath);
		var tokens = urlPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		var paths = new List<string>();

		var accumulated = "";
		for (var index = 0; index < tokens.Length; index++)
		{
			accumulated += tokens[index] + '/';
			paths.Add($"{mapping.SourcePathPrefix}/{accumulated.TrimStart('/')}");
		}

		// Add the base path_prefix itself
		paths.Add($"{mapping.SourcePathPrefix}/");

		return paths.ToArray();
	}

	/// <summary>
	/// Converts a markdown file path to a URL path by removing .md extension and /index suffixes.
	/// </summary>
	public static string MarkdownPathToUrlPath(string path)
	{
		if (path.EndsWith("/index.md", StringComparison.OrdinalIgnoreCase))
			path = path[..^8];
		if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
			path = path[..^3];
		return path;
	}
}
