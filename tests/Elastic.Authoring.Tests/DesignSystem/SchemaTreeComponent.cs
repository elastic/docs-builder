// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.DesignSystem;

public class SchemaTreeComponent : DesignSystemDirectiveTest
{
	protected override string Markdown =>
		"""
		:::{schema-tree} Response fields
		* `value` — `Struct` — nullable
		  * `child` — `Utf8` — non-null
		:::
		""";

	[Test]
	public async Task Render_Preview_UsesTheSchemaTree() => await Docs.ConvertsToHtmlSnapshot();

	[Test]
	public async Task Parse_Preview_EmitsNoErrors() => await Docs.HasNoErrors();
}
