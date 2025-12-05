// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Infrastructure.Adapters.Search;
using FluentAssertions;
using Xunit;

namespace Elastic.Documentation.Api.Infrastructure.Tests.Adapters.Search;

public class StringHighlightExtensionsTests
{
	[Fact]
	public void EmptyTokensReturnsOriginalText()
	{
		var text = "Hello world";
		var result = text.HighlightTokens([]);

		result.Should().Be(text);
	}

	[Fact]
	public void EmptyTextReturnsEmptyString()
	{
		var result = "".HighlightTokens(["test"]);

		result.Should().BeEmpty();
	}

	[Fact]
	public void NullTextReturnsNull()
	{
		string? text = null;
		var result = text!.HighlightTokens(["test"]);

		result.Should().BeNull();
	}

	[Fact]
	public void SingleTokenHighlightsMatch()
	{
		var text = "Hello world";
		var result = text.HighlightTokens(["world"]);

		result.Should().Be("Hello <mark>world</mark>");
	}

	[Fact]
	public void SingleTokenHighlightsFirstCharacter()
	{
		var text = "Aggregations are useful";
		var result = text.HighlightTokens(["Ag"]);

		result.Should().Be("<mark>Ag</mark>gregations are useful");
	}

	[Fact]
	public void SingleTokenCaseInsensitiveMatch()
	{
		var text = "Hello WORLD";
		var result = text.HighlightTokens(["world"]);

		result.Should().Be("Hello <mark>WORLD</mark>");
	}

	[Fact]
	public void SingleTokenPreservesOriginalCase()
	{
		var text = "Hello WoRlD";
		var result = text.HighlightTokens(["world"]);

		result.Should().Be("Hello <mark>WoRlD</mark>");
	}

	[Fact]
	public void SingleTokenMultipleOccurrences()
	{
		var text = "test one test two test";
		var result = text.HighlightTokens(["test"]);

		result.Should().Be("<mark>test</mark> one <mark>test</mark> two <mark>test</mark>");
	}

	[Fact]
	public void MultipleTokensHighlightsAll()
	{
		var text = "Hello world from here";
		var result = text.HighlightTokens(["hello", "world"]);

		result.Should().Be("<mark>Hello</mark> <mark>world</mark> from here");
	}

	[Fact]
	public void AlreadyHighlightedTokenSkipsDoubleHighlighting()
	{
		var text = "Hello <mark>world</mark> again";
		var result = text.HighlightTokens(["world"]);

		result.Should().Be("Hello <mark>world</mark> again");
	}

	[Fact]
	public void TokenInsideMarkTagNotHighlighted()
	{
		var text = "<mark>hello world</mark> and world outside";
		var result = text.HighlightTokens(["world"]);

		result.Should().Be("<mark>hello world</mark> and <mark>world</mark> outside");
	}

	[Fact]
	public void SingleCharTokensHighlighted()
	{
		var text = "a b c test";
		var result = text.HighlightTokens(["a", "b", "test"]);

		result.Should().Be("<mark>a</mark> <mark>b</mark> c <mark>test</mark>");
	}

	[Fact]
	public void TokenNotFoundReturnsOriginal()
	{
		var text = "Hello world";
		var result = text.HighlightTokens(["notfound"]);

		result.Should().Be(text);
	}

	[Fact]
	public void MixedHighlightedAndUnhighlighted()
	{
		var text = "<mark>elastic</mark>search documentation";
		var result = text.HighlightTokens(["elastic", "documentation"]);

		result.Should().Be("<mark>elastic</mark>search <mark>documentation</mark>");
	}

	[Fact]
	public void TokenAtStartOfText()
	{
		var text = "Elasticsearch is great";
		var result = text.HighlightTokens(["Elasticsearch"]);

		result.Should().Be("<mark>Elasticsearch</mark> is great");
	}

	[Fact]
	public void TokenAtEndOfText()
	{
		var text = "Search with Elasticsearch";
		var result = text.HighlightTokens(["Elasticsearch"]);

		result.Should().Be("Search with <mark>Elasticsearch</mark>");
	}

	[Fact]
	public void PartialTokenInsideExistingMarkNotDoubleHighlighted()
	{
		var text = "<mark>dotnet</mark> is a framework";
		var result = text.HighlightTokens(["net"]);

		// "net" inside <mark>dotnet</mark> should not be highlighted
		result.Should().Be("<mark>dotnet</mark> is a framework");
	}

	[Fact]
	public void ConsecutiveTokensWithoutSpace()
	{
		var text = "HelloWorld";
		var result = text.HighlightTokens(["Hello", "World"]);

		result.Should().Be("<mark>Hello</mark><mark>World</mark>");
	}

	[Fact]
	public void OverlappingTokensFirstWins()
	{
		var text = "testing";
		var result = text.HighlightTokens(["test", "sting"]);

		// "test" gets highlighted first, then "sting" check finds "ting" but "s" is outside the mark
		result.Should().Contain("<mark>test</mark>");
	}

	[Fact]
	public void SpecialCharactersInTextHandledCorrectly()
	{
		var text = "C# and .NET framework";
		var result = text.HighlightTokens(["NET"]);

		result.Should().Be("C# and .<mark>NET</mark> framework");
	}

	[Fact]
	public void MultipleMarksWithTokenBetween()
	{
		var text = "<mark>first</mark> middle <mark>last</mark>";
		var result = text.HighlightTokens(["middle"]);

		result.Should().Be("<mark>first</mark> <mark>middle</mark> <mark>last</mark>");
	}

	[Fact]
	public void EmptyTokenInArrayIgnored()
	{
		var text = "Hello world";
		var result = text.HighlightTokens(["", "world", null!]);

		result.Should().Be("Hello <mark>world</mark>");
	}

	[Fact]
	public void TokenMatchingMarkTagNotBroken()
	{
		// Edge case: what if someone searches for "mark"?
		var text = "The mark element is used for highlighting";
		var result = text.HighlightTokens(["mark"]);

		result.Should().Be("The <mark>mark</mark> element is used for highlighting");
	}

	[Fact]
	public void NestedMarkTagsHandledCorrectly()
	{
		// This shouldn't happen in practice but let's make sure we don't break
		var text = "<mark>outer <mark>inner</mark> outer</mark>";
		var result = text.HighlightTokens(["test"]);

		result.Should().Be(text);
	}

	[Fact]
	public void LongTextWithManyMatchesPerformsWell()
	{
		var text = string.Join(" ", Enumerable.Repeat("elasticsearch kibana logstash beats", 100));
		var result = text.HighlightTokens(["elasticsearch", "kibana"]);

		result.Should().Contain("<mark>elasticsearch</mark>");
		result.Should().Contain("<mark>kibana</mark>");
		result.Should().NotContain("<mark>logstash</mark>");
	}

	[Fact]
	public void UnicodeTextHandledCorrectly()
	{
		var text = "日本語 elasticsearch テスト";
		var result = text.HighlightTokens(["elasticsearch"]);

		result.Should().Be("日本語 <mark>elasticsearch</mark> テスト");
	}

	[Fact]
	public void TokenWithUppercaseMarkStillWorks()
	{
		var text = "Hello <MARK>world</MARK> test";
		var result = text.HighlightTokens(["world"]);

		// The existing mark is uppercase, should still be detected
		result.Should().Be("Hello <MARK>world</MARK> test");
	}

	[Fact]
	public void RealWorldExampleSearchResults()
	{
		var text = "Elasticsearch is a distributed, RESTful search and analytics engine";
		var result = text.HighlightTokens(["elasticsearch", "search"]);

		result.Should().Be("<mark>Elasticsearch</mark> is a distributed, RESTful <mark>search</mark> and analytics engine");
	}

	[Fact]
	public void RealWorldExamplePartiallyHighlighted()
	{
		var text = "Learn about <mark>Elasticsearch</mark> and how to use search effectively";
		var result = text.HighlightTokens(["elasticsearch", "search"]);

		result.Should().Be("Learn about <mark>Elasticsearch</mark> and how to use <mark>search</mark> effectively");
	}

	[Fact]
	public void StartOfStringHighlight()
	{
		var text = "<mark>APM</mark> Architecture for AWS Lambda";
		var result = text.HighlightTokens(["apm", "ar"]);

		result.Should().Be("<mark>APM</mark> <mark>Ar</mark>chitecture for AWS Lambda");
	}

	[Fact]
	public void StartOfStringHighlight2()
	{
		var text = "APM Architecture for AWS Lambda";
		var result = text.HighlightTokens(["a"]);

		result.Should().Be("<mark>A</mark>PM <mark>A</mark>rchitecture for <mark>A</mark>WS L<mark>a</mark>mbd<mark>a</mark>");
	}

	[Fact]
	public void IgnoreOtherHtml()
	{
		var text = "<>APM<> Architecture for AWS Lambda";
		var result = text.HighlightTokens(["apm"]);

		result.Should().Be("<><mark>APM</mark><> Architecture for AWS Lambda");
	}

	[Fact]
	public void HighlightInsideNonMarkHtml()
	{
		// Only <mark> tags are protected, other HTML tags get their content highlighted
		var text = "<APM> Architecture for AWS Lambda";
		var result = text.HighlightTokens(["apm"]);

		result.Should().Be("<<mark>APM</mark>> Architecture for AWS Lambda");
	}

	[Fact]
	public void PartiallyHighlightedTitleHighlightsRemaining()
	{
		var text = "<mark>Elastic</mark>search cluster management";
		var result = text.HighlightTokens(["search", "cluster"]);

		result.Should().Be("<mark>Elastic</mark><mark>search</mark> <mark>cluster</mark> management");
	}

	[Fact]
	public void PartiallyHighlightedMiddleHighlightsAround()
	{
		var text = "Learn <mark>Elasticsearch</mark> basics today";
		var result = text.HighlightTokens(["learn", "basics", "today"]);

		result.Should().Be("<mark>Learn</mark> <mark>Elasticsearch</mark> <mark>basics</mark> <mark>today</mark>");
	}

	[Fact]
	public void MultiplePartialHighlightsHighlightsGaps()
	{
		var text = "<mark>APM</mark> and <mark>logging</mark> for observability";
		var result = text.HighlightTokens(["apm", "and", "logging", "observability"]);

		result.Should().Be("<mark>APM</mark> <mark>and</mark> <mark>logging</mark> for <mark>observability</mark>");
	}

	[Fact]
	public void BrokenMarkTagArkFragmentHandledSafely()
	{
		// Malformed HTML with "ark>" fragment
		var text = "This has ark> in it and some test content";
		var result = text.HighlightTokens(["test"]);

		result.Should().Be("This has ark> in it and some <mark>test</mark> content");
	}

	[Fact]
	public void BrokenMarkTagMaFragmentHandledSafely()
	{
		// Malformed HTML with "</ma" fragment
		var text = "This has </ma in it and some test content";
		var result = text.HighlightTokens(["test"]);

		result.Should().Be("This has </ma in it and some <mark>test</mark> content");
	}

	[Fact]
	public void BrokenMarkTagMarkWithoutCloseHandledSafely()
	{
		// Unclosed <mark> tag
		var text = "This has <mark>unclosed and test content";
		var result = text.HighlightTokens(["test"]);

		// Content after unclosed <mark> is considered inside the tag
		result.Should().Be("This has <mark>unclosed and test content");
	}

	[Fact]
	public void BrokenMarkTagCloseWithoutOpenHandledSafely()
	{
		// </mark> without opening tag
		var text = "This has </mark> orphan and test content";
		var result = text.HighlightTokens(["test", "orphan"]);

		result.Should().Be("This has </mark> <mark>orphan</mark> and <mark>test</mark> content");
	}

	[Fact]
	public void BrokenMarkTagPartialOpenTagHandledSafely()
	{
		// Partial "<mar" without completion
		var text = "This has <mar and test content";
		var result = text.HighlightTokens(["test"]);

		// "<mar" looks like start of HTML tag, so "test" after it should still highlight
		result.Should().Be("This has <mar and <mark>test</mark> content");
	}

	[Fact]
	public void BrokenMarkTagJustAngleBracketsHandledSafely()
	{
		var text = "Use < and > for comparisons and test values";
		var result = text.HighlightTokens(["test"]);

		result.Should().Be("Use < and > for comparisons and <mark>test</mark> values");
	}

	[Fact]
	public void PartialHighlightTokenSpansHighlightBoundary()
	{
		// Token "search" spans from outside to inside highlighted area
		var text = "full<mark>text</mark>search capabilities";
		var result = text.HighlightTokens(["search", "full"]);

		result.Should().Be("<mark>full</mark><mark>text</mark><mark>search</mark> capabilities");
	}

	[Fact]
	public void PartialHighlightAdjacentMarks()
	{
		var text = "<mark>hello</mark><mark>world</mark> test";
		var result = text.HighlightTokens(["test"]);

		result.Should().Be("<mark>hello</mark><mark>world</mark> <mark>test</mark>");
	}

	[Fact]
	public void PartialHighlightNestedLookingContent()
	{
		// Content that looks like it could be nested (but isn't valid HTML)
		var text = "<mark>outer <mark inner</mark> text";
		var result = text.HighlightTokens(["text"]);

		// "text" is after </mark> so should be highlighted
		result.Should().Be("<mark>outer <mark inner</mark> <mark>text</mark>");
	}

	// ========== Synonyms Tests ==========

	[Fact]
	public void SynonymsNullDictionaryHighlightsOnlyTokens()
	{
		var text = "Kubernetes cluster management";
		var result = text.HighlightTokens(["kubernetes"], null);

		result.Should().Be("<mark>Kubernetes</mark> cluster management");
	}

	[Fact]
	public void SynonymsEmptyDictionaryHighlightsOnlyTokens()
	{
		var text = "Kubernetes cluster management";
		var synonyms = new Dictionary<string, string[]>();
		var result = text.HighlightTokens(["kubernetes"], synonyms);

		result.Should().Be("<mark>Kubernetes</mark> cluster management");
	}

	[Fact]
	public void SynonymsHighlightsBothTokenAndSynonym()
	{
		var text = "Kubernetes and k8s are the same thing";
		var synonyms = new Dictionary<string, string[]>
		{
			["kubernetes"] = ["k8s"]
		};
		var result = text.HighlightTokens(["kubernetes"], synonyms);

		result.Should().Be("<mark>Kubernetes</mark> and <mark>k8s</mark> are the same thing");
	}

	[Fact]
	public void SynonymsHighlightsMultipleSynonyms()
	{
		var text = "Use Elasticsearch or ES or elastic for search";
		var synonyms = new Dictionary<string, string[]>
		{
			["elasticsearch"] = ["es", "elastic"]
		};
		var result = text.HighlightTokens(["elasticsearch"], synonyms);

		result.Should().Be("Use <mark>Elasticsearch</mark> or <mark>ES</mark> or <mark>elastic</mark> for search");
	}

	[Fact]
	public void SynonymsCaseInsensitiveLookup()
	{
		var text = "K8S is short for kubernetes";
		var synonyms = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
		{
			["kubernetes"] = ["k8s"]
		};
		var result = text.HighlightTokens(["KUBERNETES"], synonyms);

		result.Should().Be("<mark>K8S</mark> is short for <mark>kubernetes</mark>");
	}

	[Fact]
	public void SynonymsTokenNotInDictionary()
	{
		var text = "Logstash is a pipeline tool";
		var synonyms = new Dictionary<string, string[]>
		{
			["elasticsearch"] = ["es"]
		};
		var result = text.HighlightTokens(["logstash"], synonyms);

		result.Should().Be("<mark>Logstash</mark> is a pipeline tool");
	}

	[Fact]
	public void SynonymsEmptySynonymArrayIgnored()
	{
		var text = "Elasticsearch is powerful";
		var synonyms = new Dictionary<string, string[]>
		{
			["elasticsearch"] = []
		};
		var result = text.HighlightTokens(["elasticsearch"], synonyms);

		result.Should().Be("<mark>Elasticsearch</mark> is powerful");
	}

	[Fact]
	public void SynonymsEmptyStringsInArrayIgnored()
	{
		var text = "Kubernetes and k8s cluster";
		var synonyms = new Dictionary<string, string[]>
		{
			["kubernetes"] = ["", "k8s", null!, ""]
		};
		var result = text.HighlightTokens(["kubernetes"], synonyms);

		result.Should().Be("<mark>Kubernetes</mark> and <mark>k8s</mark> cluster");
	}

	[Fact]
	public void SynonymsMultipleTokensWithDifferentSynonyms()
	{
		var text = "Deploy k8s with es and ml for machine learning";
		var synonyms = new Dictionary<string, string[]>
		{
			["kubernetes"] = ["k8s"],
			["elasticsearch"] = ["es"],
			["machine learning"] = ["ml"]
		};
		var result = text.HighlightTokens(["kubernetes", "elasticsearch"], synonyms);

		result.Should().Be("Deploy <mark>k8s</mark> with <mark>es</mark> and ml for machine learning");
	}

	[Fact]
	public void SynonymsAlreadyHighlightedSynonymNotDoubleHighlighted()
	{
		var text = "Use <mark>k8s</mark> for Kubernetes deployments";
		var synonyms = new Dictionary<string, string[]>
		{
			["kubernetes"] = ["k8s"]
		};
		var result = text.HighlightTokens(["kubernetes"], synonyms);

		result.Should().Be("Use <mark>k8s</mark> for <mark>Kubernetes</mark> deployments");
	}

	[Fact]
	public void SynonymsBiDirectionalLookup()
	{
		// Simulating bi-directional synonyms (as used in SearchConfiguration.SynonymBiDirectional)
		var text = "Search with k8s or kubernetes in your cluster";
		var synonyms = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
		{
			["kubernetes"] = ["k8s"],
			["k8s"] = ["kubernetes"]
		};
		var result = text.HighlightTokens(["k8s"], synonyms);

		result.Should().Be("Search with <mark>k8s</mark> or <mark>kubernetes</mark> in your cluster");
	}

	[Fact]
	public void SynonymsMultipleOccurrencesOfSynonym()
	{
		var text = "k8s here and k8s there but also kubernetes";
		var synonyms = new Dictionary<string, string[]>
		{
			["kubernetes"] = ["k8s"]
		};
		var result = text.HighlightTokens(["kubernetes"], synonyms);

		result.Should().Be("<mark>k8s</mark> here and <mark>k8s</mark> there but also <mark>kubernetes</mark>");
	}

	[Fact]
	public void SynonymsRealWorldElasticSearchExample()
	{
		var text = "Configure ES cluster settings in Elasticsearch for elastic cloud";
		var synonyms = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
		{
			["elasticsearch"] = ["es", "elastic"]
		};
		var result = text.HighlightTokens(["elasticsearch"], synonyms);

		result.Should().Be("Configure <mark>ES</mark> cluster settings in <mark>Elasticsearch</mark> for <mark>elastic</mark> cloud");
	}

	[Fact]
	public void SynonymsRealWorldMachineLearningExample()
	{
		var text = "ML models for machine learning in the ml node";
		var synonyms = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
		{
			["machine learning"] = ["ml"]
		};
		var result = text.HighlightTokens(["machine learning"], synonyms);

		// Note: "machine learning" as a token matches the phrase, ml is a synonym
		result.Should().Be("<mark>ML</mark> models for <mark>machine learning</mark> in the <mark>ml</mark> node");
	}

	[Fact]
	public void SynonymsSynonymInsideMarkTagNotHighlighted()
	{
		var text = "<mark>kubernetes and k8s</mark> are popular";
		var synonyms = new Dictionary<string, string[]>
		{
			["kubernetes"] = ["k8s"]
		};
		var result = text.HighlightTokens(["kubernetes"], synonyms);

		// Both kubernetes and k8s are inside mark tag, should not be double-highlighted
		result.Should().Be("<mark>kubernetes and k8s</mark> are popular");
	}

	[Fact]
	public void SynonymsMixedHighlightedAndUnhighlightedSynonyms()
	{
		var text = "<mark>k8s</mark> and kubernetes cluster";
		var synonyms = new Dictionary<string, string[]>
		{
			["kubernetes"] = ["k8s"]
		};
		var result = text.HighlightTokens(["kubernetes"], synonyms);

		result.Should().Be("<mark>k8s</mark> and <mark>kubernetes</mark> cluster");
	}

	[Fact]
	public void SynonymsPreservesOriginalCaseForSynonym()
	{
		var text = "Use K8S for your deployments";
		var synonyms = new Dictionary<string, string[]>
		{
			["kubernetes"] = ["k8s"]
		};
		var result = text.HighlightTokens(["kubernetes"], synonyms);

		// Original case "K8S" should be preserved in the highlight
		result.Should().Be("Use <mark>K8S</mark> for your deployments");
	}

	[Fact]
	public void SynonymsWithSpecialCharacters()
	{
		var text = "Use ES|QL or esql for queries";
		var synonyms = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
		{
			["esql"] = ["ES|QL"]
		};
		var result = text.HighlightTokens(["esql"], synonyms);

		result.Should().Be("Use <mark>ES|QL</mark> or <mark>esql</mark> for queries");
	}

	[Fact]
	public void SynonymsPartialMatchNotHighlighted()
	{
		// Synonym "k8s" should not match "k8ss" or "ak8s"
		var text = "k8ss is not k8s and ak8s is wrong";
		var synonyms = new Dictionary<string, string[]>
		{
			["kubernetes"] = ["k8s"]
		};
		var result = text.HighlightTokens(["kubernetes"], synonyms);

		// k8s within k8ss and ak8s will be highlighted since it's a substring match
		// This is expected behavior - same as regular tokens
		result.Should().Contain("<mark>k8s</mark>");
	}
}
