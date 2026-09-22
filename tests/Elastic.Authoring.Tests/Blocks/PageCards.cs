// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Blocks;

// {page-card} moved onto the shared DirectiveLinkValidator. These tests pin the contract it
// had before that move: relative links resolve against the source file, and no file-existence
// check runs, because page-card links can target generated pages with no markdown on disk.

public class PageCardWithARelativeLink : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{page-card} [Admonitions](admonitions.md)
		Callout boxes for notes and warnings.
		:::
		""";

	[Test, DisplayName("resolves the link relative to the source file")]
	public async Task ResolvesLinkRelative() =>
		await Docs.ConvertsToContainingHtml(
			"""
		<a href="/admonitions" style="text-decoration:none;" class="page-card flex items-center justify-between w-full border border-grey-20 rounded-lg px-6 py-4 mt-2 hover:border-grey-80 hover:bg-grey-5 group">
			<div class="min-w-0">
				<div class="font-semibold text-blue-elastic group-hover:underline">Admonitions</div>
				<div class="text-sm text-ink-light mt-0.5 font-normal" style="text-decoration:none;">
					<p>Callout boxes for notes and warnings.</p>
				</div>
			</div>
			<svg class="size-5 shrink-0 ml-4 text-ink-light" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
				<path stroke-linecap="round" stroke-linejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5"></path>
			</svg>
		</a>
		"""
		);

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class PageCardWithADotRelativeLink : MarkdownTest
{
	protected override string Markdown => """
		:::{page-card} [Add](./add.md)
		:::
		""";

	// The CLI reference generates page-cards pointing at generated pages that have no markdown
	// file on disk. A file-existence check here would report false positives on every one.
	[Test, DisplayName("does not check that the target file exists")]
	public async Task DoesNotCheckFileExists() => await Docs.HasNoErrors();
}

public class PageCardWithoutADescription : MarkdownTest
{
	protected override string Markdown => """
		:::{page-card} [Tables](tables.md)
		:::
		""";

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class PageCardWithAnAbsoluteUrl : MarkdownTest
{
	protected override string Markdown => """
		:::{page-card} [Elastic](https://www.elastic.co)
		:::
		""";

	[Test, DisplayName("errors")]
	public async Task Errors() => await Docs.HasError("page-card url must be a local .md path or crosslink");
}

public class PageCardWithoutAMarkdownLink : MarkdownTest
{
	protected override string Markdown => """
		:::{page-card} Admonitions
		:::
		""";

	[Test, DisplayName("errors")]
	public async Task Errors() => await Docs.HasError("page-card requires a markdown link argument");
}
