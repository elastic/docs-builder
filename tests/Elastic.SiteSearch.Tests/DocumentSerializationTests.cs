// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AutoBogus;
using AwesomeAssertions;
using Elastic.Documentation.Search.Contract;

namespace Elastic.SiteSearch.Tests;

public class DocumentSerializationTests
{
	private static readonly JsonSerializerOptions Options = new()
	{
		TypeInfoResolver = SourceGenerationContext.Default
	};

	/// <summary>
	/// Options that configure SearchDocumentBase as a fallback-safe polymorphic root.
	/// See <see cref="SearchDocumentPolymorphism.WithFallback"/>.
	/// </summary>
	private static readonly JsonSerializerOptions FallbackOptions = new()
	{
		TypeInfoResolver = JsonTypeInfoResolver.Combine(SourceGenerationContext.Default)
			.WithAddedModifier(SearchDocumentPolymorphism.WithFallback())
	};

	/// <summary>
	/// <see cref="SearchDocumentBase.ContentType"/> is JSON-driven; AutoBogus would assign unrelated strings if populated.
	/// </summary>
	private static AutoFaker<T> CreateAutoFaker<T>()
		where T : SearchDocumentBase =>
		new AutoFaker<T>().Configure(builder =>
		{
#pragma warning disable CA2263 // Skip path must match the concrete faker type T (inherited ContentType).
			builder.WithSkip(typeof(T), nameof(SearchDocumentBase.ContentType));
#pragma warning restore CA2263
		});

	[Fact]
	public void SiteDocument_Roundtrips()
	{
		var original = CreateAutoFaker<SiteDocument>().Generate();

		var json = JsonSerializer.Serialize<ISearchDocument>(original, Options);
		var deserialized = JsonSerializer.Deserialize<ISearchDocument>(json, Options);

		deserialized.Should().NotBeNull();
		deserialized.Should().BeOfType<SiteDocument>();
		deserialized.Should().BeEquivalentTo(original);
	}

	[Fact]
	public void GuideDocument_Roundtrips()
	{
		var original = CreateAutoFaker<GuideDocument>().Generate();

		var json = JsonSerializer.Serialize<ISearchDocument>(original, Options);
		var deserialized = JsonSerializer.Deserialize<ISearchDocument>(json, Options);

		deserialized.Should().NotBeNull();
		deserialized.Should().BeOfType<GuideDocument>();
		deserialized.Should().BeEquivalentTo(original);
	}

	[Fact]
	public void SiteDocument_Preserves_CrawlFields()
	{
		var original = CreateAutoFaker<SiteDocument>().Generate();

		original.HttpEtag.Should().NotBeNullOrEmpty("AutoFaker should populate HttpEtag");
		original.HttpLastModified.Should().NotBeNull("AutoFaker should populate HttpLastModified");

		var json = JsonSerializer.Serialize<ISearchDocument>(original, Options);
		var deserialized = JsonSerializer.Deserialize<ISearchDocument>(json, Options) as SiteDocument;

		deserialized.Should().NotBeNull();
		deserialized.HttpEtag.Should().Be(original.HttpEtag);
		deserialized.HttpLastModified.Should().Be(original.HttpLastModified);
	}

	[Fact]
	public void GuideDocument_Preserves_CrawlFields()
	{
		var original = CreateAutoFaker<GuideDocument>().Generate();

		original.HttpEtag.Should().NotBeNullOrEmpty("AutoFaker should populate HttpEtag");
		original.HttpLastModified.Should().NotBeNull("AutoFaker should populate HttpLastModified");

		var json = JsonSerializer.Serialize<ISearchDocument>(original, Options);
		var deserialized = JsonSerializer.Deserialize<ISearchDocument>(json, Options) as GuideDocument;

		deserialized.Should().NotBeNull();
		deserialized.HttpEtag.Should().Be(original.HttpEtag);
		deserialized.HttpLastModified.Should().Be(original.HttpLastModified);
	}

	[Fact]
	public void ConcreteRead_StaysFlat_IgnoringDiscriminator()
	{
		// Polymorphism is opt-in via the interface. A concrete-type read does NOT dispatch
		// on $type — every hit comes back as the declared concrete type. This is the
		// shape DefaultSearchService<TDocument> relies on (TDocument = WebsiteSearchDocument
		// for the unified index).
		var siteOriginal = CreateAutoFaker<SiteDocument>().Generate();
		var json = JsonSerializer.Serialize<ISearchDocument>(siteOriginal, Options);

		var flat = JsonSerializer.Deserialize<WebsiteSearchDocument>(json, Options);
		flat.Should().NotBeNull();
		flat.Should().BeOfType<WebsiteSearchDocument>(); // no dispatch — stays the declared type
	}

	[Fact]
	public void MissingDiscriminator_ReadAs_ISearchDocument_Throws()
	{
		// Polymorphism on an interface is strict: missing $type is an unrecoverable error
		// because the interface cannot be instantiated as a fallback.
		// Callers that don't have a discriminator should read as SearchDocumentBase with
		// WithFallback() — see MissingDiscriminator_ReadAs_SearchDocumentBase_ReturnsFallback.
		var json = """
		{
			"title": "Getting Started",
			"search_title": "Getting Started with Elasticsearch",
			"url": "/docs/get-started",
			"hash": "abc123"
		}
		""";

		var act = () => JsonSerializer.Deserialize<ISearchDocument>(json, Options);
		act.Should().Throw<NotSupportedException>().WithMessage("*type discriminator*");
	}

	[Fact]
	public void UnknownDiscriminator_ReadAs_ISearchDocument_Throws()
	{
		// An unknown $type on an interface root still throws because the interface cannot be
		// instantiated as a fallback, even with IgnoreUnrecognizedTypeDiscriminators=true.
		var json = """
		{
			"$type": "unknown-future-type",
			"title": "Some Page",
			"search_title": "Some Page",
			"url": "/some/page",
			"hash": "def456"
		}
		""";

		var act = () => JsonSerializer.Deserialize<ISearchDocument>(json, Options);
		act.Should().Throw<NotSupportedException>();
	}

	[Fact]
	public void MissingDiscriminator_ReadAs_SearchDocumentBase_ReturnsFallback()
	{
		// With WithFallback() applied, SearchDocumentBase is a concrete polymorphic root.
		// A missing $type materializes a SearchDocumentBase instance rather than throwing.
		var json = """
		{
			"title": "Getting Started",
			"search_title": "Getting Started with Elasticsearch",
			"url": "/docs/get-started",
			"hash": "abc123"
		}
		""";

		var fallback = JsonSerializer.Deserialize<SearchDocumentBase>(json, FallbackOptions);

		fallback.Should().NotBeNull();
		fallback.Should().BeOfType<SearchDocumentBase>();
		fallback.Title.Should().Be("Getting Started");
		fallback.Url.Should().Be("/docs/get-started");
	}

	[Fact]
	public void UnknownDiscriminator_ReadAs_SearchDocumentBase_ReturnsFallback()
	{
		// With WithFallback() applied, an unrecognized $type yields a SearchDocumentBase
		// fallback instance instead of throwing.
		var json = """
		{
			"$type": "unknown-future-type",
			"title": "Some Page",
			"search_title": "Some Page",
			"url": "/some/page",
			"hash": "def456"
		}
		""";

		var fallback = JsonSerializer.Deserialize<SearchDocumentBase>(json, FallbackOptions);

		fallback.Should().NotBeNull();
		fallback.Should().BeOfType<SearchDocumentBase>();
		fallback.Title.Should().Be("Some Page");
	}

	[Fact]
	public void KnownDiscriminator_ReadAs_SearchDocumentBase_WithFallback_DispatchesToConcreteType()
	{
		// Even with WithFallback(), a known $type still dispatches to the correct concrete type.
		var json = """
		{
			"$type": "site",
			"title": "Blog Post",
			"search_title": "Blog Post",
			"url": "/blog/post",
			"hash": "abc"
		}
		""";

		var result = JsonSerializer.Deserialize<SearchDocumentBase>(json, FallbackOptions);

		result.Should().NotBeNull();
		result.Should().BeOfType<SiteDocument>();
	}

	[Fact]
	public void ContentType_FromJson_Overrides_WhenPresent()
	{
		var json = """
		{
			"title": "Legacy",
			"search_title": "Legacy",
			"url": "/x",
			"hash": "h",
			"content_type": "archived-site"
		}
		""";

		// Read as the concrete type — flat, no polymorphic dispatch, ContentType survives.
		var deserialized = JsonSerializer.Deserialize<SiteDocument>(json, Options);

		deserialized.Should().NotBeNull();
		deserialized.Type.Should().Be("site");
		deserialized.ContentType.Should().Be("archived-site");
	}

	[Fact]
	public void SiteDocument_Type_IsHardcoded()
	{
		var doc = CreateAutoFaker<SiteDocument>().Generate();
		doc.Type.Should().Be("site");
	}

	[Fact]
	public void GuideDocument_Type_IsHardcoded()
	{
		var doc = CreateAutoFaker<GuideDocument>().Generate();
		doc.Type.Should().Be("guide");
	}

	[Fact]
	public void NavigationFields_Roundtrip()
	{
		var json = """
		{
			"$type": "site",
			"title": "Test",
			"search_title": "Test",
			"url": "/blog/my-post",
			"hash": "abc",
			"navigation_section": "blog",
			"navigation_depth": 2,
			"navigation_table_of_contents": 5,
			"ai_autocomplete_questions": ["search elasticsearch", "how to index", "query dsl basics"]
		}
		""";

		var doc = JsonSerializer.Deserialize<ISearchDocument>(json, Options) as SiteDocument;

		doc.Should().NotBeNull();
		doc.NavigationSection.Should().Be("blog");
		doc.NavigationDepth.Should().Be(2);
		doc.NavigationTableOfContents.Should().Be(5);
		doc.AiAutocompleteQuestions.Should().BeEquivalentTo(["search elasticsearch", "how to index", "query dsl basics"]);

		var reserialised = JsonSerializer.Serialize<ISearchDocument>(doc, Options);
		using var el = JsonDocument.Parse(reserialised);
		el.RootElement.GetProperty("navigation_section").GetString().Should().Be("blog");
		el.RootElement.GetProperty("navigation_depth").GetInt32().Should().Be(2);
		el.RootElement.GetProperty("navigation_table_of_contents").GetInt32().Should().Be(5);
	}

	[Fact]
	public void NavigationFields_DefaultPenaltyValues()
	{
		var doc = new SiteDocument
		{
			Title = "Test",
			SearchTitle = "Test",
			Url = "/x",
			Hash = "h"
		};

		// rank_feature defaults to 50 so documents without explicit nav metadata are penalised
		doc.NavigationDepth.Should().Be(50);
		doc.NavigationTableOfContents.Should().Be(50);

		var json = JsonSerializer.Serialize<SiteDocument>(doc, Options);
		using var el = JsonDocument.Parse(json);
		el.RootElement.GetProperty("navigation_depth").GetInt32().Should().Be(50);
		el.RootElement.GetProperty("navigation_table_of_contents").GetInt32().Should().Be(50);
		el.RootElement.TryGetProperty("navigation_section", out _).Should().BeFalse();
		el.RootElement.TryGetProperty("ai_autocomplete_questions", out _).Should().BeFalse();
	}

	[Fact]
	public void SiteDocument_IncludesDiscriminator_InJson()
	{
		var original = CreateAutoFaker<SiteDocument>().Generate();

		var json = JsonSerializer.Serialize<ISearchDocument>(original, Options);
		using var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("$type", out var typeProp).Should().BeTrue();
		typeProp.GetString().Should().Be("site");
		doc.RootElement.TryGetProperty("content_type", out var contentTypeProp).Should().BeTrue();
		contentTypeProp.GetString().Should().Be("site");
	}

	[Fact]
	public void GuideDocument_IncludesDiscriminator_InJson()
	{
		var original = CreateAutoFaker<GuideDocument>().Generate();

		var json = JsonSerializer.Serialize<ISearchDocument>(original, Options);
		using var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("$type", out var typeProp).Should().BeTrue();
		typeProp.GetString().Should().Be("guide");
		doc.RootElement.TryGetProperty("content_type", out var contentTypeProp).Should().BeTrue();
		contentTypeProp.GetString().Should().Be("guide");
	}

	[Fact]
	public void ConcreteSerialize_Omits_Discriminator()
	{
		// Serialize<WebsiteSearchDocument>() (or any concrete type) doesn't emit $type because
		// the declared type has no [JsonPolymorphic]. Useful when writing into a uniform index.
		var original = CreateAutoFaker<WebsiteSearchDocument>().Generate();

		var json = JsonSerializer.Serialize<WebsiteSearchDocument>(original, Options);
		using var doc = JsonDocument.Parse(json);

		doc.RootElement.TryGetProperty("$type", out _).Should().BeFalse();
		doc.RootElement.TryGetProperty("content_type", out var ct).Should().BeTrue();
		ct.GetString().Should().Be("website");
	}

	[Fact]
	public void Compose_WithFallback_CombinesContractAndConsumerContexts()
	{
		// Simulate a consumer (e.g. docs-builder) composing the contract resolver with its own
		// context and opting into the fallback. We use the contract's own context as both the
		// contract and "consumer" to keep the test self-contained.
		var composed = SearchDocumentPolymorphism.Compose(
			consumerContexts: [SourceGenerationContext.Default],
			SearchDocumentPolymorphism.WithFallback()
		);
		var options = new JsonSerializerOptions { TypeInfoResolver = composed };

		// Known $type still dispatches correctly when reading as SearchDocumentBase.
		var guideJson = """{"$type":"guide","title":"t","search_title":"t","url":"/g","hash":"h"}""";
		var guide = JsonSerializer.Deserialize<SearchDocumentBase>(guideJson, options);
		guide.Should().BeOfType<GuideDocument>();

		// Unknown $type returns a SearchDocumentBase fallback.
		var unknownJson = """{"$type":"future-type","title":"t","search_title":"t","url":"/g","hash":"h"}""";
		var fallback = JsonSerializer.Deserialize<SearchDocumentBase>(unknownJson, options);
		fallback.Should().BeOfType<SearchDocumentBase>();
		fallback.Title.Should().Be("t");

		// Missing $type returns a SearchDocumentBase fallback.
		var missingJson = """{"title":"t","search_title":"t","url":"/g","hash":"h"}""";
		var missing = JsonSerializer.Deserialize<SearchDocumentBase>(missingJson, options);
		missing.Should().BeOfType<SearchDocumentBase>();
	}

	[Fact]
	public void Compose_ViaISearchDocument_KnownTypeDispatchesCorrectly()
	{
		// Verify that ISearchDocument-based dispatch still works with the composed resolver.
		// The composed resolver's contract context handles [JsonPolymorphic] on ISearchDocument.
		var composed = SearchDocumentPolymorphism.Compose(
			consumerContexts: [SourceGenerationContext.Default],
			SearchDocumentPolymorphism.WithFallback()
		);
		var options = new JsonSerializerOptions { TypeInfoResolver = composed };

		// ISearchDocument dispatch: guide → GuideDocument
		var guideJson = """{"$type":"guide","title":"t","search_title":"t","url":"/g","hash":"h"}""";
		var guide = JsonSerializer.Deserialize<ISearchDocument>(guideJson, options);
		guide.Should().BeOfType<GuideDocument>();

		// ISearchDocument dispatch: site → SiteDocument
		var siteJson = """{"$type":"site","title":"t","search_title":"t","url":"/s","hash":"h"}""";
		var site = JsonSerializer.Deserialize<ISearchDocument>(siteJson, options);
		site.Should().BeOfType<SiteDocument>();

		// ISearchDocument dispatch: labs → LabsDocument
		var labsJson = """{"$type":"labs","title":"t","search_title":"t","url":"/l","hash":"h"}""";
		var labs = JsonSerializer.Deserialize<ISearchDocument>(labsJson, options);
		labs.Should().BeOfType<LabsDocument>();

		// ISearchDocument dispatch: website → WebsiteSearchDocument
		var websiteJson = """{"$type":"website","title":"t","search_title":"t","url":"/w","hash":"h"}""";
		var website = JsonSerializer.Deserialize<ISearchDocument>(websiteJson, options);
		website.Should().BeOfType<WebsiteSearchDocument>();
	}

	[Fact]
	public void Compose_ViaISearchDocument_UnknownDiscriminator_Throws()
	{
		// ISearchDocument cannot be instantiated as a fallback (it is an interface), so an
		// unknown $type must still throw even with WithFallback() applied (which only configures
		// the fallback on SearchDocumentBase, not on ISearchDocument).
		var composed = SearchDocumentPolymorphism.Compose(
			consumerContexts: [SourceGenerationContext.Default],
			SearchDocumentPolymorphism.WithFallback()
		);
		var options = new JsonSerializerOptions { TypeInfoResolver = composed };
		var json = """{"$type":"future-type","title":"t","search_title":"t","url":"/g","hash":"h"}""";

		var act = () => JsonSerializer.Deserialize<ISearchDocument>(json, options);
		act.Should().Throw<NotSupportedException>();
	}

	[Fact]
	public void Compose_ViaISearchDocument_MissingDiscriminator_Throws()
	{
		// Same as above: ISearchDocument with no $type must throw because the interface cannot
		// be instantiated, regardless of WithFallback() being applied.
		var composed = SearchDocumentPolymorphism.Compose(
			consumerContexts: [SourceGenerationContext.Default],
			SearchDocumentPolymorphism.WithFallback()
		);
		var options = new JsonSerializerOptions { TypeInfoResolver = composed };
		var json = """{"title":"t","search_title":"t","url":"/g","hash":"h"}""";

		var act = () => JsonSerializer.Deserialize<ISearchDocument>(json, options);
		act.Should().Throw<NotSupportedException>();
	}
}
