// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links;

namespace Elastic.Authoring.Tests.Inline;

// The cross-link resolver reports a link's problems at the link, under the id of the LinkGraphConstraints rule that covers them; the graph check
// that runs after the build does not ask those rules again (LinkGraphIsolatedConstraints), so each problem appears once.

public class UndeclaredCrossLinkSchemeIsReportedOnceUnderM34 : MarkdownTest
{
	protected override string Markdown =>
		"""
		[APM Server binary](docs-x:/solutions/observability/apps/apm-server-binary.md)
		""";

	[Test, DisplayName("the resolver's message, once, under the rule's id")]
	public async Task ReportedOnce() =>
		await Docs.HasRuleDiagnosticOnce(
			LinkGraphConstraints.CrossLinkSchemeDeclared.Name,
			Severity.Error,
			"'docs-x' was not found in the cross link index"
		);
}

public class MissingCrossLinkPageIsReportedOnceUnderM30 : MarkdownTest
{
	protected override string Markdown => """
		[Non-existent file](docs-content://non-existent-file.md)
		""";

	[Test, DisplayName("the resolver's message, once, under the rule's id")]
	public async Task ReportedOnce() =>
		await Docs.HasRuleDiagnosticOnce(
			LinkGraphConstraints.CrossLinkResolves.Name,
			Severity.Error,
			"'non-existent-file.md' is not a valid link in the 'docs-content' cross link index"
		);
}

public class MissingCrossLinkAnchorIsReportedOnceUnderM30 : MarkdownTest
{
	protected override string Markdown =>
		"""
		[APM Server binary](docs-content:/solutions/observability/apps/apm-server-binary.md#apm-deb-x)
		""";

	[Test, DisplayName("the resolver's message, once, under the rule's id")]
	public async Task ReportedOnce() =>
		await Docs.HasRuleDiagnosticOnce(
			LinkGraphConstraints.CrossLinkResolves.Name,
			Severity.Error,
			"'solutions/observability/apps/apm-server-binary.md' has no anchor named: '#apm-deb-x'"
		);
}
