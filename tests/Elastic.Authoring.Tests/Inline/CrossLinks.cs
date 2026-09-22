// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Inline;

public class CrossLinkMakesItIntoHtml : MarkdownTest
{
	protected override string Markdown =>
		"""
		[APM Server binary](docs-content:/solutions/observability/apps/apm-server-binary.md)
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<p><a
		    href="https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/solutions/observability/apps/apm-server-binary"
		    target="_blank"
		    rel="noopener noreferrer">
		    APM Server binary
		    </a>
		</p>
		"""
		);

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();

	[Test, DisplayName("has no warning")]
	public async Task HasNoWarnings() => await Docs.HasNoWarnings();
}

public class ErrorWhenUsingWrongScheme : MarkdownTest
{
	protected override string Markdown =>
		"""
		[APM Server binary](docs-x:/solutions/observability/apps/apm-server-binary.md)
		""";

	[Test, DisplayName("error on bad scheme")]
	public async Task ErrorOnBadScheme() => await Docs.HasError("'docs-x' was not found in the cross link index");

	[Test, DisplayName("has no warning")]
	public async Task HasNoWarnings() => await Docs.HasNoWarnings();
}

public class ErrorWhenBadAnchorIsUsed : MarkdownTest
{
	protected override string Markdown =>
		"""
		[APM Server binary](docs-content:/solutions/observability/apps/apm-server-binary.md#apm-deb-x)
		""";

	[Test, DisplayName("error when linking to unknown anchor")]
	public async Task ErrorWhenLinkingToUnknownAnchor() =>
		await Docs.HasError("'solutions/observability/apps/apm-server-binary.md' has no anchor named: '#apm-deb-x");

	[Test, DisplayName("has no warning")]
	public async Task HasNoWarnings() => await Docs.HasNoWarnings();
}

public class LinkToValidAnchor : MarkdownTest
{
	protected override string Markdown =>
		"""
		[APM Server binary](docs-content:/solutions/observability/apps/apm-server-binary.md#apm-deb)
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<p><a
		    href="https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/solutions/observability/apps/apm-server-binary#apm-deb"
		    target="_blank"
		    rel="noopener noreferrer">
		    APM Server binary
		    </a>
		</p>
		"""
		);

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();

	[Test, DisplayName("has no warning")]
	public async Task HasNoWarnings() => await Docs.HasNoWarnings();
}

public class LinkToAnchorWithDifferentCasing : MarkdownTest
{
	protected override string Markdown =>
		"""
		[Whitelist](docs-content:/solutions/observability/apps/apm-server-binary.md#elasticsearch-requestheaderswhitelist)
		""";

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();

	[Test, DisplayName("has no warning")]
	public async Task HasNoWarnings() => await Docs.HasNoWarnings();
}

public class LinkToRepositoryThatDoesNotResolveYet : MarkdownTest
{
	protected override string Markdown => """
		[Elasticsearch Documentation](elasticsearch:/index.md)
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<p><a
		    href="elasticsearch:/index.md">
		    Elasticsearch Documentation
		    </a>
		</p>
		"""
		);

	[Test, DisplayName("error when not found in links.json")]
	public async Task ErrorWhenNotFound() => await Docs.HasError("'elasticsearch' was not found in the cross link index");

	[Test, DisplayName("has no warning")]
	public async Task HasNoWarnings() => await Docs.HasNoWarnings();
}

public class ErrorWhenLinkingToNonExistentFileInDeclaredRepository : MarkdownTest
{
	protected override string Markdown => """
		[Non-existent file](docs-content://non-existent-file.md)
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<p><a
		    href="docs-content://non-existent-file.md">
		    Non-existent file
		    </a>
		</p>
		"""
		);

	[Test, DisplayName("error when file not found in links.json")]
	public async Task ErrorWhenFileNotFound() =>
		await Docs.HasError("'non-existent-file.md' is not a valid link in the 'docs-content' cross link index");

	[Test, DisplayName("has no warning")]
	public async Task HasNoWarnings() => await Docs.HasNoWarnings();
}

public class UsingDoubleForwardSlashes : MarkdownTest
{
	protected override string Markdown =>
		"""
		[APM Server binary](docs-content://solutions/observability/apps/apm-server-binary.md#apm-deb)
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<p><a
		    href="https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/solutions/observability/apps/apm-server-binary#apm-deb"
		    target="_blank"
		    rel="noopener noreferrer">
		    APM Server binary
		    </a>
		</p>
		"""
		);

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();

	[Test, DisplayName("has no warning")]
	public async Task HasNoWarnings() => await Docs.HasNoWarnings();
}

public class LinkToRepositoryThatDoesNotResolveYetUsingDoubleSlashes : MarkdownTest
{
	protected override string Markdown => """
		[Elasticsearch Documentation](elasticsearch://index.md)
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<p><a
		    href="elasticsearch://index.md">
		    Elasticsearch Documentation
		    </a>
		</p>
		"""
		);

	[Test, DisplayName("error when not found in links.json")]
	public async Task ErrorWhenNotFound() => await Docs.HasError("'elasticsearch' was not found in the cross link index");

	[Test, DisplayName("has no warning")]
	public async Task HasNoWarnings() => await Docs.HasNoWarnings();
}
