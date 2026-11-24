// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Search.Highlighting;

namespace Elastic.Documentation.Api.Tests;

public class HighlightEncodingTests
{
	private const string Hit =
		"""
		{"took":1,"timed_out":false,"_shards":{"total":1,"successful":1,"failed":0},"hits":{"total":{"value":1,"relation":"eq"},"hits":[
		{"_index":"test","_id":"tenant","_score":1,
		"_source":{"title":"The <tenant> parameter","search_title":"The <tenant> parameter","path":"/tenant","description":"Set the <tenant> & region."},
		"highlight":{"body":["Send a <mark>ping</mark> to <tenant>"]}}]}}
		""";

	[Test]
	public void ProcessHit_WithHighlightOptions_EncodesLiteralTextAndKeepsMarks()
	{
		var result = SearchResultProcessor.ProcessHit(
			ApiLookupResultTests.ParseResponse(Hit).Hits.Single(),
			"tenant",
			new Dictionary<string, string[]>(),
			new HighlightOptions()
		);

		result.Title.Should().Be("The &lt;<mark>tenant</mark>&gt; parameter");
		result.Description.Should().Be("Send a <mark>ping</mark> to &lt;<mark>tenant</mark>&gt;");
	}

	[Test]
	public void ProcessHit_WithoutHighlightOptions_KeepsPlainText()
	{
		var result = SearchResultProcessor.ProcessHit(
			ApiLookupResultTests.ParseResponse(Hit).Hits.Single(),
			string.Empty,
			new Dictionary<string, string[]>()
		);

		result.Title.Should().Be("The <tenant> parameter");
	}

	[Test]
	public void ProcessHit_TypedPlaceholder_MarksItsEncodedText()
	{
		var result = SearchResultProcessor.ProcessHit(
			ApiLookupResultTests.ParseResponse(Hit).Hits.Single(),
			"<tenant>",
			new Dictionary<string, string[]>(),
			new HighlightOptions()
		);

		result.Title.Should().Be("The <mark>&lt;tenant&gt;</mark> parameter");
	}

	[Test]
	[Arguments("lt")]
	[Arguments("amp")]
	[Arguments("quot")]
	public void HighlightTokens_TokenInsideAnEntity_LeavesTheEntityWhole(string token) =>
		"a &lt;b&gt; &amp; &quot;c&quot;".HighlightTokens([token]).Should().Be("a &lt;b&gt; &amp; &quot;c&quot;");

	[Test]
	public void EncodeAroundMarks_LiteralTagsAndAmpersands_AreEncodedOutsideMarks() =>
		"<b>&</b> <mark>x<y</mark> \"q\"".EncodeAroundMarks().Should().Be("&lt;b&gt;&amp;&lt;/b&gt; <mark>x&lt;y</mark> &quot;q&quot;");
}
