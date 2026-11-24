// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Search.Highlighting;

namespace Elastic.Documentation.Api.Tests;

public class HighlightEncodingTests
{
	private static readonly Dictionary<string, string[]> NoSynonyms = [];

	// What the HTML encoder returns: the document text encoded, only the highlighter's own marks raw.
	private const string Highlighted =
		"""
		{"took":1,"timed_out":false,"_shards":{"total":1,"successful":1,"failed":0},"hits":{"total":{"value":1,"relation":"eq"},"hits":[
		{"_index":"test","_id":"tenant","_score":1,
		"_source":{"title":"The <tenant> parameter","search_title":"The <tenant> parameter","path":"/tenant","description":"Set the <tenant> & region."},
		"highlight":{"body":["Use &lt;mark&gt; to <mark>ping</mark> a &lt;tenant&gt;"]}}]}}
		""";

	// What a request without highlighting returns.
	private const string Plain =
		"""
		{"took":1,"timed_out":false,"_shards":{"total":1,"successful":1,"failed":0},"hits":{"total":{"value":1,"relation":"eq"},"hits":[
		{"_index":"test","_id":"tenant","_score":1,
		"_source":{"title":"The <tenant> parameter","search_title":"The <tenant> parameter","path":"/tenant","description":"Set the <tenant> & region."}}]}}
		""";

	private static SearchResultItem<DocumentationDocument> Process(
		string json,
		string query,
		IReadOnlyDictionary<string, string[]>? synonyms = null,
		HighlightOptions? options = null
	) => SearchResultProcessor.ProcessHit(ApiLookupResultTests.ParseResponse(json).Hits.Single(), query, synonyms ?? NoSynonyms, options);

	[Test]
	public void ProcessHit_WithHighlightOptions_EncodesLiteralTextAndKeepsMarks()
	{
		var result = Process(Highlighted, "tenant", options: new HighlightOptions());

		result.Title.Should().Be("The &lt;<mark>tenant</mark>&gt; parameter");
		result.Description.Should().Be("Use &lt;mark&gt; to <mark>ping</mark> a &lt;<mark>tenant</mark>&gt;");
	}

	[Test]
	public void ProcessHit_WithoutHighlighting_KeepsThePlainTextOfTitleAndDescription()
	{
		var result = Process(Plain, string.Empty);

		result.Title.Should().Be("The <tenant> parameter");
		result.Description.Should().Be("Set the <tenant> & region.");
	}

	[Test]
	public void ProcessHit_TypedPlaceholder_MarksItsEncodedText()
	{
		var result = Process(Plain, "<tenant>", options: new HighlightOptions());

		result.Title.Should().Be("The <mark>&lt;tenant&gt;</mark> parameter");
	}

	[Test]
	public void ProcessHit_LiteralMarkInTheDocumentTitle_StaysText()
	{
		var result = Process(Plain.Replace("The <tenant> parameter", "Use <mark> tags"), "tags", options: new HighlightOptions());

		result.Title.Should().Be("Use &lt;mark&gt; <mark>tags</mark>");
	}

	[Test]
	[Arguments("R&D", "research")]
	[Arguments("research", "R&D")]
	public void ProcessHit_SynonymWithAnAmpersand_IsMarkedBothWays(string typed, string synonym)
	{
		var json = Plain.Replace("The <tenant> parameter", "R&D and research");

		var result = Process(json, typed, new Dictionary<string, string[]> { [typed] = [synonym] }, new HighlightOptions());

		result.Title.Should().Be("<mark>R&amp;D</mark> and <mark>research</mark>");
	}

	[Test]
	[Arguments("lt")]
	[Arguments("amp")]
	[Arguments("quot")]
	public void HighlightTokens_TokenInsideAnEntity_LeavesTheEntityWhole(string token) =>
		"a &lt;b&gt; &amp; &quot;c&quot;".HighlightTokens([token]).Should().Be("a &lt;b&gt; &amp; &quot;c&quot;");

	[Test]
	public void ReencodeHighlight_EncoderOutput_KeepsOnlyTheHighlighterMarks() =>
		"&lt;mark&gt;&amp;&#x2F; <mark>x&lt;y</mark> &quot;q&quot;"
			.ReencodeHighlight()
			.Should()
			.Be("&lt;mark&gt;&amp;/ <mark>x&lt;y</mark> &quot;q&quot;");
}
