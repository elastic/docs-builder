// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Inline.InlineLinks;

public class InlineLinkWithMailto : MarkdownTest
{
	protected override string Markdown => """
		[email me](mailto:fake-email@elastic.co)
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml("""
		<p>
			<a href="mailto:fake-email@elastic.co">email me</a>
		</p>
		""");

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();

	[Test, DisplayName("has no warning")]
	public async Task HasNoWarnings() => await Docs.HasNoWarnings();
}

public class InlineLinkWithMailtoNotAllowedExternalHost : MarkdownTest
{
	protected override string Markdown => """
		[email me](mailto:fake-email@somehost.co)
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml("""
		<p>
			<a href="mailto:fake-email@somehost.co">email me</a>
		</p>
		""");

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();

	[Test, DisplayName("has error")]
	public async Task HasError() => await Docs.HasWarning("mailto links should be to elastic.co domains.");
}

public class EmptyLinkShouldResultInAnError : MarkdownTest
{
	protected override string Markdown => """
		[email me]()
		""";

	[Test, DisplayName("has error")]
	public async Task HasError() => await Docs.HasError("Found empty url");

	[Test, DisplayName("has no warnings")]
	public async Task HasNoWarnings() => await Docs.HasNoWarnings();
}
