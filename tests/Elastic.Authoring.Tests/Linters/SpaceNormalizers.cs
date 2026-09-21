// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Linters.SpaceNormalizers;

public class SpaceDetection : MarkdownTest
{
	// \u000B is a vertical tab (VT), which is an irregular space character the linter flags.
	protected override string Markdown => "not a\u000Bspace\n";

	[Test, DisplayName("validate HTML: should not contain bad space character")]
	public async Task ValidateHtml() => await Docs.ConvertsToHtml("<p>not a space</p>");

	[Test, DisplayName("emits a hint when a bad space is used")]
	public async Task EmitsHint() =>
		await Docs.HasHint("Irregular space detected. Run 'docs-builder format --write' to automatically fix all instances.");
}
