// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Page;

internal static class PageTitleResolver
{
	/// <summary>The HTML title: the <c>meta_title</c> override when set, otherwise the page title, followed by the site name.</summary>
	public static string Resolve(string title, string? overrideTitle, string siteName) =>
		$"{(string.IsNullOrWhiteSpace(overrideTitle) ? title : overrideTitle)} | {siteName}";
}
