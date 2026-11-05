// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Snapshots.Html;

namespace Elastic.Documentation.Testing;

/// <summary>
/// How docs-builder's tests normalise rendered HTML before comparing or snapshotting it. One place for what
/// <c>Elastic.Markdown.Tests.PrettyHtmlExtensions</c> and <c>Elastic.Authoring.Tests.Framework.HtmlAssertions</c>
/// used to do separately.
/// </summary>
public static class DocsHtml
{
	/// <summary>htmx navigation attributes on links are plumbing, not content.</summary>
	private static readonly string[] HtmxAttributes = ["hx-get", "hx-select-oob", "hx-swap", "hx-indicator", "hx-push-url", "preload"];

	/// <summary>The whole body, with htmx attributes removed from links.</summary>
	public static HtmlSnapshotOptions Body() => new HtmlSnapshotOptions().RemoveAttributesFrom("a", HtmxAttributes);

	/// <summary>
	/// The rendered page's content section, without the leading H1 the test fixtures wrap every document in
	/// (<c># Test Document</c>), and with htmx attributes removed from links.
	/// </summary>
	public static HtmlSnapshotOptions Section() =>
		Body()
			.Select("section#elastic-docs-v3")
			.Transform(root =>
			{
				if (root.FirstElementChild is { TagName: "H1" } h1)
					h1.Remove();
			});
}
