// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Search.Contract.Mapping;
using Elastic.Mapping.Analysis;

namespace Elastic.SiteSearch.Tests;

/// <summary>
/// Verifies the synonym-aware analyzers built by <see cref="SharedAnalysisFactory"/>, in
/// particular the "c#"/".net" symbol-rewrite char_filter attached ahead of tokenization.
/// </summary>
public class SharedAnalysisFactoryTests
{
	private static JsonElement BuildAnalysisJson()
	{
		var settings = SharedAnalysisFactory.BuildAnalysis(new AnalysisBuilder(), "test_synonyms", ["ml => machine learning"]).Build();
		using var doc = JsonDocument.Parse(settings.ToJson().ToJsonString());
		return doc.RootElement.Clone();
	}

	[Fact]
	public void SynonymsFixedAnalyzer_HasSymbolRewriteCharFilterBeforeTokenization()
	{
		var json = BuildAnalysisJson();
		var analyzer = json.GetProperty("analyzer").GetProperty("synonyms_fixed_analyzer");
		var charFilters = analyzer.GetProperty("char_filter").EnumerateArray().Select(e => e.GetString()).ToArray();
		charFilters.Should().Contain("symbol_rewrite_char_filter");
	}

	[Fact]
	public void SynonymsAnalyzer_HasSymbolRewriteCharFilterBeforeTokenization()
	{
		var json = BuildAnalysisJson();
		var analyzer = json.GetProperty("analyzer").GetProperty("synonyms_analyzer");
		var charFilters = analyzer.GetProperty("char_filter").EnumerateArray().Select(e => e.GetString()).ToArray();
		charFilters.Should().Contain("symbol_rewrite_char_filter");
	}

	[Fact]
	public void SymbolRewriteCharFilter_IsPatternReplaceToDotnet()
	{
		var json = BuildAnalysisJson();
		var charFilter = json.GetProperty("char_filter").GetProperty("symbol_rewrite_char_filter");
		charFilter.GetProperty("type").GetString().Should().Be("pattern_replace");
		charFilter.GetProperty("replacement").GetString().Should().Be("dotnet");
	}

	[Fact]
	public void SynonymsFixedAnalyzer_HasMorphologyOverrideBeforeKstem()
	{
		var json = BuildAnalysisJson();
		var filters = json.GetProperty("analyzer").GetProperty("synonyms_fixed_analyzer")
			.GetProperty("filter").EnumerateArray().Select(e => e.GetString()).ToArray();
		filters.Should().ContainInOrder("lowercase", "morphology_override_filter", "synonyms_fixed_filter", "kstem");
	}

	[Fact]
	public void SynonymsAnalyzer_HasMorphologyOverrideBeforeKstem()
	{
		var json = BuildAnalysisJson();
		var filters = json.GetProperty("analyzer").GetProperty("synonyms_analyzer")
			.GetProperty("filter").EnumerateArray().Select(e => e.GetString()).ToArray();
		filters.Should().ContainInOrder("lowercase", "morphology_override_filter", "synonyms_filter", "kstem");
	}

	[Fact]
	public void MorphologyOverrideFilter_IsStemmerOverrideWithCuratedRules()
	{
		var json = BuildAnalysisJson();
		var filter = json.GetProperty("filter").GetProperty("morphology_override_filter");
		filter.GetProperty("type").GetString().Should().Be("stemmer_override");
		var rules = filter.GetProperty("rules").EnumerateArray().Select(e => e.GetString()).ToArray();
		rules.Should().BeEquivalentTo([
			"config, configuration => config",
			"install, installation => install",
			"auth, authentication => auth",
		]);
	}

	[Fact]
	public void ContentTagsAnalyzer_UsesKeywordTokenizerWithKstemAndAliasSynonyms()
	{
		var json = BuildAnalysisJson();
		var analyzer = json.GetProperty("analyzer").GetProperty("content_tags_analyzer");
		analyzer.GetProperty("tokenizer").GetString().Should().Be("keyword");
		var filters = analyzer.GetProperty("filter").EnumerateArray().Select(e => e.GetString()).ToArray();
		filters.Should().ContainInOrder("lowercase", "kstem", "content_tags_synonyms_filter");
	}

	[Fact]
	public void ContentTagsSynonymsFilter_OnlyCoversWhatStemmingCannotFold()
	{
		var json = BuildAnalysisJson();
		var filter = json.GetProperty("filter").GetProperty("content_tags_synonyms_filter");
		filter.GetProperty("type").GetString().Should().Be("synonym_graph");
		var synonyms = filter.GetProperty("synonyms").EnumerateArray().Select(e => e.GetString()).ToArray();
		// Regular plurals (labs/blogs/webinars) are already folded by kstem above -- only the
		// hyphen-vs-space normalization needs an explicit rule.
		synonyms.Should().BeEquivalentTo(["customer story, customer-story"]);
	}
}
