// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Inline;

// These record what link handling does today, for every shape of link in one page, so a refactor of the link
// model has to keep the diagnostics and the rendered hrefs exactly as they are. The scenarios use made-up pages.

static file class Pages
{
	private const string Redirects =
		"""
		redirects:
		  'old/moved.md': 'guide/page.md'
		  'old/moved-anchors.md':
		    to: 'guide/page.md'
		    anchors: { 'old-anchor': 'known-section' }
		  'old/split.md':
		    many:
		      - to: 'guide/page.md'
		        anchors: { 'a': 'known-section' }
		      - to: 'guide/other.md'
		        anchors: { 'b': 'other-section' }
		  'testing/redirects/first-page-old.md': 'docs-content://get-started/index.md'
		  'testing/redirects/anchor-old.md':
		    to: 'docs-content://get-started/index.md'
		    anchors: { 'old': 'elasticsearch-intro-use-cases' }
		  'testing/redirects/missing-target-old.md': 'docs-content://nope.md'
		""";

	public static IReadOnlyCollection<TestFile> With(string indexMarkdown) =>
		[
			TestFile.Index(indexMarkdown),
			TestFile.Page(
				"guide/page.md",
				"""
			# Guide page

			## Known section [#known-section]

			Text.
			"""
			),
			TestFile.Page("guide/other.md", """
			# Other page

			## Other section [#other-section]
			"""),
			TestFile.Page("guide/my file.md", "# Spaces\n"),
			TestFile.Static("images/pic.png"),
			TestFile.Static("files/doc.pdf"),
			new("redirects.yml", Redirects),
			new("_redirects.yml", Redirects)
		];
}

public class InlineLinkMatrix : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		Pages.With(
			"""
			# Links

			[relative](guide/page.md)
			[dot relative](./guide/page.md)
			[absolute path](/guide/page.md)
			[with anchor](guide/page.md#known-section)
			[unknown anchor](guide/page.md#nope)
			[anchor on index](#links)
			[no extension](guide/page)
			[relative missing](guide/missing.md)
			[absolute missing](/guide/missing.md)
			[escaped spaces](guide/my%20file.md)
			[angle spaces](<guide/my file.md>)
			[exists but is not published](files/doc.pdf)
			[verbatim](!/guide/page.md)
			[empty url]()
			[](guide/page.md)
			[](guide/page.md#known-section)
			[](guide/missing.md)
			[moved](old/moved.md)
			[moved with anchors](old/moved-anchors.md#old-anchor)
			[split redirect](old/split.md#a)
			![image](images/pic.png)
			![missing image](images/missing.png)
			![image with size](images/pic.png "A title =100x50")
			"""
		);

	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() =>
		await Docs.HasDiagnostics(
			"""
		error {repo}/docs/index.md:7 `nope` does not exist in guide/page.md.
		error {repo}/docs/index.md:8 `links` does not exist in index.md.
		error {repo}/docs/index.md:9 `guide/page` is not a valid internal link. Did you forget to add the .md extension?
		error {repo}/docs/index.md:10 `guide/missing.md` does not exist. If it was recently removed add a redirect. resolved to `{repo}/docs/guide/missing.md`
		error {repo}/docs/index.md:11 `/guide/missing.md` does not exist. If it was recently removed add a redirect. resolved to `{repo}/docs/guide/missing.md`
		error {repo}/docs/index.md:12 `guide/my%20file.md` does not exist: links are not URL-decoded, so `%20` is matched literally. The file name contains spaces: write the link with spaces instead, e.g. `[text](<my file.png>)`, or rename the file to remove them. Only markdown files and images are published, so for any other file type (such as a PDF) link to its raw GitHub URL (https://raw.githubusercontent.com/<org>/<repo>/<branch>/<path>).
		warning {repo}/docs/index.md:14 `files/doc.pdf` exists in the repository but is not published with the documentation, so this link will be broken after deploy. Only markdown files and images (png, jpg, jpeg, gif, svg) are published. Link to the file using its raw GitHub URL instead (https://raw.githubusercontent.com/<org>/<repo>/<branch>/<path>).
		error {repo}/docs/index.md:16 Found empty url
		error {repo}/docs/index.md:19 `guide/missing.md` does not exist. If it was recently removed add a redirect. resolved to `{repo}/docs/guide/missing.md`
		warning {repo}/docs/index.md:19 'guide/missing.md' could not be resolved to a markdown file while creating an auto text link, '{repo}/docs/guide/missing.md' does not exist.
		warning {repo}/docs/index.md:20 Local file `old/moved.md` has a redirect, please update this reference to: guide/page.md
		warning {repo}/docs/index.md:21 Local file `old/moved-anchors.md` has a redirect, please update this reference to: guide/page.md
		warning {repo}/docs/index.md:22 Local file `old/split.md` has a redirect, please update this reference to: one of: guide/page.md, guide/other.md
		error {repo}/docs/index.md:24 `images/missing.png` does not exist. If it was recently removed add a redirect. resolved to `{repo}/docs/images/missing.png`
		"""
		);

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() => await Docs.ConvertsToHtmlSnapshot();
}

public class ExternalAndTemplatedLinkMatrix : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		Pages.With(
			"""
			---
			sub:
			  host: https://example.test
			---
			# External links

			[https](https://example.test/page)
			[http](http://example.test/page)
			[elastic.co mailto](mailto:someone@elastic.co)
			[other mailto](mailto:someone@example.test)
			[substituted]({{host}}/page)
			[unresolved]({{missing-sub}}/page)
			[resolved to a path]({{path}}/page)
			<https://example.test/autolink>
			"""
		);

	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() =>
		await Docs.HasDiagnostics(
			"""
			warning {repo}/docs/index.md:10 mailto links should be to elastic.co domains. Found example.test in mailto:someone@example.test. 
			error {repo}/docs/index.md:12 The url contains unresolved template expressions: '{{missing-sub}}/page'. Please check if there is an appropriate global or frontmatter subs variable.
			error {repo}/docs/index.md:13 The url contains unresolved template expressions: '{{path}}/page'. Please check if there is an appropriate global or frontmatter subs variable.
			"""
		);

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() => await Docs.ConvertsToHtmlSnapshot();
}

public class CrossLinkMatrix : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		Pages.With(
			"""
			# Cross links

			[plain](docs-content://get-started/index.md)
			[single slash](docs-content:/get-started/index.md)
			[anchor](docs-content://get-started/index.md#elasticsearch-intro-use-cases)
			[anchor with other casing](docs-content://get-started/index.md#Elasticsearch-Intro-Use-Cases)
			[unknown anchor](docs-content://get-started/index.md#nope)
			[unknown page](docs-content://nope.md)
			[index](docs-content://index.md)
			[other repository](kibana://get-started/index.md)
			[undeclared scheme](unknown-repo://get-started/index.md)
			[declared without references](elasticsearch://index.md)
			[](docs-content://get-started/index.md)
			[cursor](cursor://file/path)
			[vscode](vscode://settings)
			[redirected](docs-content://testing/redirects/first-page-old.md)
			[redirected with anchor](docs-content://testing/redirects/anchor-old.md#old)
			[redirected with unmapped anchor](docs-content://testing/redirects/anchor-old.md#other)
			[redirect to a missing page](docs-content://testing/redirects/missing-target-old.md)
			"""
		);

	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() =>
		await Docs.HasDiagnostics(
			"""
			error {repo}/docs/index.md:7 'get-started/index.md' has no anchor named: '#nope'.
			error {repo}/docs/index.md:8 'nope.md' is not a valid link in the 'docs-content' cross link index: https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/docs-builder-tests/docs-content/links.json
			error {repo}/docs/index.md:11 'unknown-repo' was not found in the cross link index. Ensure it is listed under 'cross_links' in your docset.yml
			error {repo}/docs/index.md:12 'elasticsearch' was not found in the cross link index. Ensure it is listed under 'cross_links' in your docset.yml
			error {repo}/docs/index.md:13 Crosslink 'docs-content://get-started/index.md' has empty link text. Please provide explicit link text.
			error {repo}/docs/index.md:18 Redirect rule for 'testing/redirects/anchor-old.md' in 'docs-content' found, but top-level rule did not handle anchor '#other'.
			error {repo}/docs/index.md:19 Redirect target 'docs-content://nope.md' points to file 'nope.md/' which was not found in repository 'docs-content's links.json.
			"""
		);

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() => await Docs.ConvertsToHtmlSnapshot();
}

public class DirectiveLinkMatrix : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		Pages.With(
			"""
			# Directive links

			::::{card-group}
			:title: Links

			:::{link-card}
			title: Absolute path
			link: /guide/page.md
			:::

			:::{link-card}
			title: Absolute path with anchor
			link: /guide/page.md#known-section
			:::

			:::{link-card}
			title: Missing absolute path
			link: /guide/missing.md
			:::

			:::{link-card}
			title: Relative path
			link: guide/page.md
			:::

			:::{link-card}
			title: Without extension
			link: /guide/page
			:::

			:::{link-card}
			title: External
			link: https://example.test/page
			:::

			:::{link-card}
			title: Anchor only
			link: '#links'
			:::

			:::{link-card}
			title: Cross link
			link: docs-content://get-started/index.md
			links:
			  - label: With anchor
			    url: docs-content://get-started/index.md#elasticsearch-intro-use-cases
			  - label: Unknown anchor
			    url: docs-content://get-started/index.md#nope
			  - label: Unknown page
			    url: docs-content://nope.md
			  - label: Undeclared
			    url: unknown-repo://page.md
			  - label: Redirected path
			    url: /old/moved.md
			  - label: Passthrough
			    url: cursor://file/path
			:::
			::::

			:::{page-card} [Relative](guide/page.md)
			:::

			:::{page-card} [Cross link](docs-content://get-started/index.md)
			:::

			:::{page-card} [Unknown cross link](docs-content://nope.md)
			:::

			:::{page-card} [Undeclared](unknown-repo://page.md)
			:::
			"""
		);

	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() =>
		await Docs.HasDiagnostics(
			"""
			error {repo}/docs/index.md:16 Directive link `/guide/missing.md` does not exist. If it was recently removed add a redirect.
			error {repo}/docs/index.md:21 Directive link `guide/page.md` must be an absolute path starting with `/`, a cross-link scheme (for example `kibana://`), or an external URL.
			error {repo}/docs/index.md:41 'get-started/index.md' has no anchor named: '#nope'.
			error {repo}/docs/index.md:41 'nope.md' is not a valid link in the 'docs-content' cross link index: https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/docs-builder-tests/docs-content/links.json
			error {repo}/docs/index.md:41 Directive link `unknown-repo://page.md` uses cross-link scheme `unknown-repo://` which is not declared under `cross_links` in docset.yml.
			warning {repo}/docs/index.md:41 Directive link `/old/moved.md` has a redirect; update to: guide/page.md
			error {repo}/docs/index.md:66 'nope.md' is not a valid link in the 'docs-content' cross link index: https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/docs-builder-tests/docs-content/links.json
			error {repo}/docs/index.md:69 Directive link `unknown-repo://page.md` uses cross-link scheme `unknown-repo://` which is not declared under `cross_links` in docset.yml.
			"""
		);

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() => await Docs.ConvertsToHtmlSnapshot();
}
