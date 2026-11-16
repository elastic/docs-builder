// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Testing;
using JetBrains.Annotations;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests;

/// <summary>HTML comparisons on Nullean.Snapshots.Html: structural, so attribute order and whitespace never fail a test.</summary>
public static class PrettyHtmlExtensions
{
	private static HtmlSnapshotOptions Options(bool sanitize) => sanitize ? DocsHtml.Body() : new HtmlSnapshotOptions();

	public static void ShouldBeHtml(
		[LanguageInjection("html")] this string actual,
		[LanguageInjection("html")] string expected,
		bool sanitize = true
	) => actual.Trim('\n').Should().BeHtml(expected.Trim('\n'), Options(sanitize));

	public static void ShouldContainHtml(
		[LanguageInjection("html")] this string actual,
		[LanguageInjection("html")] string expected,
		bool sanitize = true
	) => actual.Trim('\n').Should().ContainHtml(expected.Trim('\n'), Options(sanitize));
}
