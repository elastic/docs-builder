// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// Maps an API page URL to the sibling HTML and CommonMark files the generator writes.
/// </summary>
public static class ApiOutputPaths
{
	public static string MarkdownUrl(string pageUrl)
	{
		var trimmed = pageUrl.TrimEnd('/');
		return trimmed.Length == 0 ? "/index.md" : trimmed + ".md";
	}

	public static string RelativeHtmlFile(string pageUrl, string? urlPathPrefix)
	{
		var fileName = Regex.Replace(pageUrl.TrimEnd('/') + "/index.html", $"^{urlPathPrefix}", string.Empty);
		return fileName.Trim('/');
	}

	public static string RelativeMarkdownFile(string pageUrl, string? urlPathPrefix)
	{
		var fileName = Regex.Replace(pageUrl.TrimEnd('/') + ".md", $"^{urlPathPrefix}", string.Empty);
		return fileName.Trim('/');
	}
}
