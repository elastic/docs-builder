// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Export;

namespace Elastic.ApiExplorer.Tests;

public class ApiPagePlainTextTests
{
	[Test]
	[Arguments("[Core](/docs/api/elasticsearch/v8/group/endpoint-core)", "Core")]
	[Arguments("Send a `<ping>` first.", "Send a <ping> first.")]
	[Arguments("Send a <ping> first.", "Send a <ping> first.")]
	public void Line_MarkdownOrPlainText_IsPlainTextWithoutLinkTargets(string text, string expected) =>
		ApiPagePlainText.Line(text).Should().Be(expected);
}
