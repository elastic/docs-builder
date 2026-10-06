// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// Isolated fixtures use <c>docs-builder-{product}</c> keys so they do not collide with
/// assembler / docs-content. Outside the assembler the prefix is dropped from the URL.
/// </summary>
public static class IsolatedApiAliases
{
	public const string FixturePrefix = "docs-builder-";

	public static bool IsFixtureKey(string apiKey) => apiKey.StartsWith(FixturePrefix, StringComparison.Ordinal);

	public static string UrlKey(string apiKey) => IsFixtureKey(apiKey) ? apiKey[FixturePrefix.Length..] : apiKey;
}
