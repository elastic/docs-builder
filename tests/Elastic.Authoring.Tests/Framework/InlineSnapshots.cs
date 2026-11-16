// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;
using Nullean.Snapshots.Json;

namespace Elastic.Authoring.Tests.Framework;

/// <summary>
/// Where a <see cref="Scenario"/> or <see cref="MarkdownResultTask"/> method was called from in a test. Inline snapshots are
/// rewritten at the call site, so a wrapper has to hand the compiler-filled caller information on to the library
/// and name itself (not <c>MatchInlineSnapshot</c>) as the method <c>snap</c> looks for in the source.
/// </summary>
internal readonly record struct CallSite(string File, string Member, int Line, string? Expression)
{
	internal InlineSnapshotCallSite For(string method) => new(new SnapshotCallSite(File, Member, Line), method, Expression);
}

/// <summary>Inline snapshot assertions for the text a documentation page renders to, in each format the harness compares.</summary>
internal static class InlineSnapshots
{
	internal static void Text(string actual, string? expected, string method, CallSite site) =>
		Match(actual, expected, SnapshotFormats.Text, method, site);

	internal static void Html(string html, string? expected, string method, CallSite site) =>
		Match(HtmlSnapshot.Pretty(html, Documentation.Testing.DocsHtml.Section()), expected, HtmlSnapshot.Format, method, site);

	internal static void Json(string json, string? expected, string method, CallSite site) =>
		Match(JsonSnapshot.Pretty(json), expected, JsonSnapshot.Format, method, site);

	private static void Match(string actual, string? expected, SnapshotFormat format, string method, CallSite site) =>
		SnapshotAssertions.MatchInline(actual.Should(), actual, expected, format, site.For(method), null, string.Empty);
}
