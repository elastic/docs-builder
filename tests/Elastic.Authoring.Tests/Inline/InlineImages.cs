// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Inline.InlineImages;

public class StaticPathToImage : MarkdownTest
{
	protected override string Markdown => """
		![Elasticsearch](/_static/img/observability.png)
		""";

	[Fact(DisplayName = "validate HTML: generates link and alt attr")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<p><img src="/_static/img/observability.png" alt="Elasticsearch" title="Elasticsearch" /></p>
		"""
		);
}

public class RelativePathToImage : MarkdownTest
{
	protected override string Markdown => """
		![Elasticsearch](_static/img/observability.png)
		""";

	[Fact(DisplayName = "validate HTML: preserves relative path")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<p><img src="/_static/img/observability.png" alt="Elasticsearch" title="Elasticsearch" /></p>
		"""
		);
}

public class SupplyingATitle : MarkdownTest
{
	protected override string Markdown => """
		![Elasticsearch](_static/img/observability.png "Hello world")
		""";

	[Fact(DisplayName = "validate HTML: includes title")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<p><img src="/_static/img/observability.png" alt="Elasticsearch" title="Elasticsearch" /></p>
		"""
		);
}

public class SupplyingATitleWithWidthAndHeight : MarkdownTest
{
	protected override string Markdown => """
		![o](obs.png "Title =250x400")
		""";

	[Fact(DisplayName = "validate HTML: does not include width and height in title")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml("""
		<p><img src="/obs.png" width="250px" height="400px" alt="o" title="o"/></p>
		""");
}

public class SupplyingATitleWithWidthAndHeightInPercentage : MarkdownTest
{
	protected override string Markdown => """
		![o](obs.png "Title =50%x40%")
		""";

	[Fact(DisplayName = "validate HTML: does not include width and height in title")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml("""
		<p><img src="/obs.png" width="50%" height="40%" alt="o" title="o"/></p>
		""");
}

public class SupplyingATitleWithWidthOnly : MarkdownTest
{
	protected override string Markdown => """
		![o](obs.png "Title =30%")
		""";

	[Fact(DisplayName = "validate HTML: sets height to width if not supplied")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml("""
		<p><img src="/obs.png" width="30%" height="30%" alt="o" title="o"/></p>
		""");
}
