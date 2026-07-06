// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using AwesomeAssertions;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Search.Contract.Mapping;
using Elastic.Mapping;
using Elastic.Mapping.Analysis;
using Elastic.Mapping.Mappings;

namespace Elastic.SiteSearch.Tests;

// -- Subtypes with an extra field -------------------------------------------

public record SiteDocumentWithExtraField : SiteDocument
{
	[Keyword]
	[JsonPropertyName("custom_tag")]
	public string? CustomTag { get; set; }
}

public record GuideDocumentWithExtraField : GuideDocument
{
	[Keyword]
	[JsonPropertyName("custom_tag")]
	public string? CustomTag { get; set; }
}

// -- Subtypes with an extra AI field ----------------------------------------

public record SiteDocumentWithExtraAiField : SiteDocument
{
	[AiField("A short AI-generated category label.")]
	[Keyword]
	[JsonPropertyName("ai_category")]
	public string? AiCategory { get; set; }
}

public record GuideDocumentWithExtraAiField : GuideDocument
{
	[AiField("A short AI-generated category label.")]
	[Keyword]
	[JsonPropertyName("ai_category")]
	public string? AiCategory { get; set; }
}

// -- Config wrappers for extended types (delegates to same field logic) ------

public class SiteExtraFieldLexicalConfig : IConfigureElasticsearch<SiteDocumentWithExtraField>
{
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => analysis;
	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<SiteDocumentWithExtraField> ConfigureMappings(MappingsBuilder<SiteDocumentWithExtraField> mappings) => mappings
		.AddSearchDocumentMappings();
}

public class SiteExtraAiFieldLexicalConfig : IConfigureElasticsearch<SiteDocumentWithExtraAiField>
{
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => analysis;
	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<SiteDocumentWithExtraAiField> ConfigureMappings(MappingsBuilder<SiteDocumentWithExtraAiField> mappings) => mappings
		.AddSearchDocumentMappings();
}

public class GuideExtraFieldLexicalConfig : IConfigureElasticsearch<GuideDocumentWithExtraField>
{
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => analysis;
	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<GuideDocumentWithExtraField> ConfigureMappings(MappingsBuilder<GuideDocumentWithExtraField> mappings) => mappings
		.AddSearchDocumentMappings();
}

public class GuideExtraAiFieldLexicalConfig : IConfigureElasticsearch<GuideDocumentWithExtraAiField>
{
	public AnalysisBuilder ConfigureAnalysis(AnalysisBuilder analysis) => analysis;
	public IReadOnlyDictionary<string, string>? IndexSettings => null;

	public MappingsBuilder<GuideDocumentWithExtraAiField> ConfigureMappings(MappingsBuilder<GuideDocumentWithExtraAiField> mappings) => mappings
		.AddSearchDocumentMappings();
}

// -- Test mapping contexts --------------------------------------------------

/// <summary>
/// Mirrors production SiteMappingContext exactly — same type, same config.
/// Hash should match the real context.
/// </summary>
[ElasticsearchMappingContext]
[Index<SiteDocument>(
	NameTemplate = "site-{type}.lexical-{env}",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(SiteLexicalConfig)
)]
public static partial class TestSiteMappingContext;

/// <summary>
/// Mirrors production GuideMappingContext exactly — same type, same config.
/// Hash should match the real context.
/// </summary>
[ElasticsearchMappingContext]
[Index<GuideDocument>(
	NameTemplate = "guide-{type}.lexical-{env}",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(GuideLexicalConfig)
)]
public static partial class TestGuideMappingContext;

/// <summary>SiteDocument + extra keyword field. Hash must differ from base.</summary>
[ElasticsearchMappingContext]
[Index<SiteDocumentWithExtraField>(
	NameTemplate = "site-{type}.lexical-{env}",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(SiteExtraFieldLexicalConfig)
)]
public static partial class SiteExtraFieldMappingContext;

/// <summary>SiteDocument + extra AI field. Hash must differ from base.</summary>
[ElasticsearchMappingContext]
[Index<SiteDocumentWithExtraAiField>(
	NameTemplate = "site-{type}.lexical-{env}",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(SiteExtraAiFieldLexicalConfig)
)]
public static partial class SiteExtraAiFieldMappingContext;

/// <summary>GuideDocument + extra keyword field. Hash must differ from base.</summary>
[ElasticsearchMappingContext]
[Index<GuideDocumentWithExtraField>(
	NameTemplate = "guide-{type}.lexical-{env}",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(GuideExtraFieldLexicalConfig)
)]
public static partial class GuideExtraFieldMappingContext;

/// <summary>GuideDocument + extra AI field. Hash must differ from base.</summary>
[ElasticsearchMappingContext]
[Index<GuideDocumentWithExtraAiField>(
	NameTemplate = "guide-{type}.lexical-{env}",
	DatePattern = "yyyy.MM.dd.HHmmss",
	Configuration = typeof(GuideExtraAiFieldLexicalConfig)
)]
public static partial class GuideExtraAiFieldMappingContext;

// -- Tests ------------------------------------------------------------------

public class MappingHashTests
{
	// ── Baseline: re-registering the same type + config produces the same hash ─

	[Test]
	public void SiteDocument_SameConfig_ProducesSameHash() =>
		TestSiteMappingContext.SiteDocument.Hash
			.Should().Be(SiteMappingContext.SiteDocument.Hash);

	[Test]
	public void GuideDocument_SameConfig_ProducesSameHash() =>
		TestGuideMappingContext.GuideDocument.Hash
			.Should().Be(GuideMappingContext.GuideDocument.Hash);

	// ── Adding a keyword field changes the hash ───────────────────────────────

	[Test]
	public void SiteDocument_ExtraField_ChangesHash() =>
		SiteExtraFieldMappingContext.SiteDocumentWithExtraField.Hash
			.Should().NotBe(SiteMappingContext.SiteDocument.Hash);

	[Test]
	public void GuideDocument_ExtraField_ChangesHash() =>
		GuideExtraFieldMappingContext.GuideDocumentWithExtraField.Hash
			.Should().NotBe(GuideMappingContext.GuideDocument.Hash);

	// ── Adding an AI field changes the hash ───────────────────────────────────

	[Test]
	public void SiteDocument_ExtraAiField_ChangesHash() =>
		SiteExtraAiFieldMappingContext.SiteDocumentWithExtraAiField.Hash
			.Should().NotBe(SiteMappingContext.SiteDocument.Hash);

	[Test]
	public void GuideDocument_ExtraAiField_ChangesHash() =>
		GuideExtraAiFieldMappingContext.GuideDocumentWithExtraAiField.Hash
			.Should().NotBe(GuideMappingContext.GuideDocument.Hash);

	// ── Extra field vs extra AI field are different from each other ────────────

	[Test]
	public void SiteDocument_ExtraField_DiffersFrom_ExtraAiField() =>
		SiteExtraFieldMappingContext.SiteDocumentWithExtraField.Hash
			.Should().NotBe(SiteExtraAiFieldMappingContext.SiteDocumentWithExtraAiField.Hash);

	[Test]
	public void GuideDocument_ExtraField_DiffersFrom_ExtraAiField() =>
		GuideExtraFieldMappingContext.GuideDocumentWithExtraField.Hash
			.Should().NotBe(GuideExtraAiFieldMappingContext.GuideDocumentWithExtraAiField.Hash);
}
