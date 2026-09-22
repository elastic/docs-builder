// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Inline;

// Base URL used in all redirect assertions
static file class Constants
{
	public const string UrlPrefix = "https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main";
}

public class LinkToRedirectedPage : MarkdownTest
{
	protected override string Markdown =>
		"""
		[Was first is now second](docs-content://testing/redirects/first-page-old.md)
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			$"""
		<p><a href="{Constants.UrlPrefix}/testing/redirects/second-page"
		    target="_blank"
		    rel="noopener noreferrer">
		    Was first is now second
		</a></p>
		"""
		);

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();

	[Test, DisplayName("has no warning")]
	public async Task HasNoWarnings() => await Docs.HasNoWarnings();
}

public class LinkToRedirectedPageWithRenamedAnchor : MarkdownTest
{
	protected override string Markdown =>
		"""
		[Was first is now second](docs-content://testing/redirects/first-page-old.md#old-anchor)
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			$"""
		<p><a href="{Constants.UrlPrefix}/testing/redirects/second-page#active-anchor"
		    target="_blank"
		    rel="noopener noreferrer">
		    Was first is now second
		</a></p>
		"""
		);

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();

	[Test, DisplayName("has no warning")]
	public async Task HasNoWarnings() => await Docs.HasNoWarnings();
}

/// <summary>Goal: A writer moves a file to a new location.
/// Required functionality: A 1:1 redirect from the old file location to the new file location.
/// All anchors in the old file are mapped to identical anchors in the new file.</summary>
public class Scenario1MovingAFile : MarkdownTest
{
	protected override string Markdown =>
		"""
		[Scenario 1](docs-content://testing/redirects/first-page-old.md#old-anchor)
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			$"""
		<p><a href="{Constants.UrlPrefix}/testing/redirects/second-page#active-anchor"
		    target="_blank"
		    rel="noopener noreferrer">
		    Scenario 1</a></p>
		"""
		);
}

public class Scenario1BMovingAFile : MarkdownTest
{
	protected override string Markdown => """
		[Scenario 1](docs-content://testing/redirects/4th-page.md#yy)
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			$"""
		<p><a href="{Constants.UrlPrefix}/testing/redirects/5th-page#yy"
		    target="_blank"
		    rel="noopener noreferrer">
		    Scenario 1</a></p>
		"""
		);
}

/// <summary>Goal: A writer breaks up an existing page into multiple pages.
/// Required functionality: A 1:many redirect, where the original file maps to multiple new pages.
/// In this case, the anchors in the old file may end up in multiple new files.
/// The ability to specify anchor-by-anchor redirects is required.</summary>
public class Scenario2SplittingAPageIntoMultipleSmallerPages : MarkdownTest
{
	protected override string Markdown =>
		"""
		[Scenario 2](docs-content://testing/redirects/second-page-old.md#aa)
		[Scenario 2](docs-content://testing/redirects/second-page-old.md#yy)
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			$"""
		<p><a href="{Constants.UrlPrefix}/testing/redirects/second-page#zz"
		    target="_blank"
		    rel="noopener noreferrer">Scenario 2</a>
		<a href="{Constants.UrlPrefix}/testing/redirects/third-page#bb"
		    target="_blank"
		    rel="noopener noreferrer">Scenario 2</a></p>
		"""
		);
}

/// <summary>Goal: A writer removes a section of a page that was previously linked to via a cross-repo anchor link.
/// Required functionality: A 1:null mapping for anchors. Any inbound links with that anchor should update
/// to point to the base page instead.</summary>
public class Scenario3DeletingASectionOnAPage : MarkdownTest
{
	protected override string Markdown =>
		"""
		[Scenario 3](docs-content://testing/redirects/third-page.md#removed-anchor)
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			$"""
		<p><a href="{Constants.UrlPrefix}/testing/redirects/third-page"
		    target="_blank"
		    rel="noopener noreferrer">
		    Scenario 3</a></p>
		"""
		);
}

/// <summary>Goal: A writer removes a section of a page that was previously linked to via a cross-repo anchor link.
/// Required functionality: A 1:null mapping for anchors. Any inbound links with that anchor should update
/// to point to the base page instead.</summary>
public class Scenario3BLinkingToARemovedAnchorOnARedirectedPage : MarkdownTest
{
	protected override string Markdown =>
		"""
		[Scenario 3 B](docs-content://testing/redirects/second-page-old.md#removed-anchor)
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			$"""
		<p><a href="{Constants.UrlPrefix}/testing/redirects/second-page"
		    target="_blank"
		    rel="noopener noreferrer">
		    Scenario 3 B</a></p>
		"""
		);
}

/// <summary>Goal: A writer removes a page completely.
/// Required functionality: A catchall redirect that strips any anchor links to the old page and points
/// to a designated fallback page instead.</summary>
public class Scenario4DeletingAnEntirePage : MarkdownTest
{
	protected override string Markdown => """
		[Scenario 4](docs-content://testing/redirects/7th-page.md#yy)
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			$"""
		<p><a href="{Constants.UrlPrefix}/testing/redirects/5th-page"
		    target="_blank"
		    rel="noopener noreferrer">
		    Scenario 4</a></p>
		"""
		);
}

public class Scenario4BDeletingAnEntirePageShortSyntax : MarkdownTest
{
	protected override string Markdown => """
		[Scenario 4](docs-content://testing/redirects/9th-page.md)
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			$"""
		<p><a href="{Constants.UrlPrefix}/testing/redirects/5th-page"
		    target="_blank"
		    rel="noopener noreferrer">
		    Scenario 4</a></p>
		"""
		);
}

/// <summary>Goal: A writer removes a page completely.
/// Redirect to empty (index) because no alternative page is available.</summary>
public class Scenario5DeletingAnEntirePage : MarkdownTest
{
	protected override string Markdown => """
		[Scenario 5](docs-content://testing/redirects/6th-page.md#yy)
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			$"""
		<p><a href="{Constants.UrlPrefix}/"
		    target="_blank"
		    rel="noopener noreferrer">Scenario 5</a></p>
		"""
		);
}
