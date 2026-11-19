// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Logging;
using WebMarkupMin.Core;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// Minifies a rendered API page. The largest operation pages repeat a nested row partial thousands of times and pass
/// Googlebot's 2 MB HTML limit, so whitespace and other bytes a browser ignores are removed before the page is written.
/// </summary>
public static class ApiHtmlMinifier
{
	/// <summary>The minified page, or the page as rendered when the minifier reports an error, so a page is never lost.</summary>
	public static string Minify(string html, string url, ILogger logger)
	{
		// A new minifier per page: one shared instance serializes Minify behind a lock, and pages render in parallel.
		var result = new HtmlMinifier(Settings()).Minify(
			html.Replace(UntilFound, UntilFoundStandIn, StringComparison.Ordinal),
			generateStatistics: false
		);
		if (result.Errors.Count == 0)
			return result.MinifiedContent.Replace(UntilFoundStandIn, UntilFound, StringComparison.Ordinal);

		var error = result.Errors[0];
		logger.LogWarning(
			"Writing API page {Url} unminified: {Category} at line {Line}, column {Column}: {Message}",
			url,
			error.Category,
			error.LineNumber,
			error.ColumnNumber,
			error.Message
		);
		return html;
	}

	// WebMarkupMin treats hidden as a plain boolean and rewrites hidden="until-found" to hidden, which stops find in page
	// opening folded rows. The value is swapped for an attribute it leaves alone, and back. Text never holds the raw
	// attribute: Razor encodes its quotes. Remove the swap once https://github.com/Taritsyn/WebMarkupMin/issues/185 ships.
	private const string UntilFound = " hidden=\"until-found\"";
	private const string UntilFoundStandIn = " data-api-hidden-until-found";

	// Quotes and optional end tags cost about 3% together, and keeping them leaves the markup as the views write it.
	private static HtmlMinificationSettings Settings() =>
		new() { AttributeQuotesRemovalMode = HtmlAttributeQuotesRemovalMode.KeepQuotes, RemoveOptionalEndTags = false };
}
