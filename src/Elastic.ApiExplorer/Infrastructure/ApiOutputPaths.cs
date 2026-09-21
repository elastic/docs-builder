// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// Maps an API page URL to the sibling HTML, CommonMark, and OpenAPI spec files the generator writes.
/// </summary>
public static class ApiOutputPaths
{
	public static string MarkdownUrl(string pageUrl) => SiblingUrl(pageUrl, ".md");

	public static string JsonUrl(string pageUrl) => SiblingUrl(pageUrl, ".json");

	public static string YamlUrl(string pageUrl) => SiblingUrl(pageUrl, ".yaml");

	public static string RelativeHtmlFile(string pageUrl, string? urlPathPrefix)
	{
		var fileName = Regex.Replace(pageUrl.TrimEnd('/') + "/index.html", $"^{urlPathPrefix}", string.Empty);
		return fileName.Trim('/');
	}

	public static string RelativeMarkdownFile(string pageUrl, string? urlPathPrefix) => RelativeSiblingFile(pageUrl, urlPathPrefix, ".md");

	public static string RelativeJsonFile(string pageUrl, string? urlPathPrefix) => RelativeSiblingFile(pageUrl, urlPathPrefix, ".json");

	public static string RelativeYamlFile(string pageUrl, string? urlPathPrefix) => RelativeSiblingFile(pageUrl, urlPathPrefix, ".yaml");

	private static string SiblingUrl(string pageUrl, string extension)
	{
		var trimmed = pageUrl.TrimEnd('/');
		return trimmed.Length == 0 ? "/index" + extension : trimmed + extension;
	}

	private static string RelativeSiblingFile(string pageUrl, string? urlPathPrefix, string extension)
	{
		var fileName = Regex.Replace(pageUrl.TrimEnd('/') + extension, $"^{urlPathPrefix}", string.Empty);
		return fileName.Trim('/');
	}
}
