# Phase 4: Indexing

## Objective
Create Elasticsearch exporters with multilingual mapping and AI enrichment integration.

## Tasks

### 4.1 Guide Indexer Exporter

**File**: `/src/tooling/crawl-indexer/Indexing/GuideIndexerExporter.cs`

Reuses existing channel patterns for `DocumentationDocument`.

```csharp
public class GuideIndexerExporter : IAsyncDisposable
{
    private readonly ElasticsearchLexicalIngestChannel<DocumentationDocument> _lexicalChannel;
    private readonly ElasticsearchSemanticIngestChannel<DocumentationDocument>? _semanticChannel;
    private readonly ElasticsearchEnrichmentCache? _enrichmentCache;
    private readonly ElasticsearchLlmClient? _llmClient;
    private readonly HtmlToMarkdownConverter _markdownConverter;
    private readonly DateTimeOffset _batchIndexDate;

    public GuideIndexerExporter(
        ILoggerFactory logFactory,
        IDiagnosticsCollector collector,
        DocumentationEndpoints endpoints,
        bool enableAiEnrichment,
        bool enableSemantic)
    {
        _batchIndexDate = DateTimeOffset.UtcNow;
        _markdownConverter = new HtmlToMarkdownConverter();

        // Initialize channels with guide-specific index names
        // Pattern: guide-lexical-{timestamp}, guide-semantic-{timestamp}
    }

    public async ValueTask StartAsync(CancellationToken ct)
    {
        // 1. Bootstrap index (create if not exists)
        // 2. Load enrichment cache
        // 3. Setup synonyms
    }

    public async ValueTask<bool> IndexDocumentAsync(DocumentationDocument document, CancellationToken ct)
    {
        // 1. Set batch date
        document.BatchIndexDate = _batchIndexDate;

        // 2. Compute content hash
        document.Hash = ComputeHash(document);

        // 3. Generate enrichment key
        var markdownBody = _markdownConverter.Convert(document.Body ?? "");
        document.EnrichmentKey = EnrichmentKeyGenerator.Generate(document.Title, markdownBody);

        // 4. Try AI enrichment
        if (_enrichmentCache != null && _llmClient != null)
        {
            var cached = _enrichmentCache.TryGet(document.EnrichmentKey);
            if (cached != null)
            {
                ApplyEnrichment(document, cached);
            }
            else
            {
                var enrichment = await _llmClient.EnrichAsync(document.Title, markdownBody, ct);
                if (enrichment != null)
                {
                    ApplyEnrichment(document, enrichment);
                    await _enrichmentCache.SetAsync(document.EnrichmentKey, enrichment, ct);
                }
            }
        }

        // 5. Write to channels
        await _lexicalChannel.WriteAsync(document);
        return true;
    }

    public async ValueTask StopAsync(CancellationToken ct)
    {
        // 1. Drain channels
        // 2. Reindex to semantic (if enabled)
        // 3. Delete stale documents
        // 4. Apply enrich policy
    }

    private static string ComputeHash(DocumentationDocument doc) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{doc.Url}|{doc.StrippedBody}|{doc.Product?.Version}|{string.Join(",", doc.Headings)}"
        )).ToHexString();
}
```

### 4.2 Site Indexer Exporter

**File**: `/src/tooling/crawl-indexer/Indexing/SiteIndexerExporter.cs`

Handles multilingual mapping for `SiteDocument`.

```csharp
public class SiteIndexerExporter : IAsyncDisposable
{
    private readonly ElasticsearchLexicalIngestChannel<SiteDocument> _lexicalChannel;
    private readonly ElasticsearchSemanticIngestChannel<SiteDocument>? _semanticChannel;
    private readonly ElasticsearchEnrichmentCache? _enrichmentCache;
    private readonly ElasticsearchLlmClient? _llmClient;
    private readonly HtmlToMarkdownConverter _markdownConverter;
    private readonly DateTimeOffset _batchIndexDate;

    public SiteIndexerExporter(
        ILoggerFactory logFactory,
        IDiagnosticsCollector collector,
        DocumentationEndpoints endpoints,
        bool enableAiEnrichment,
        bool enableSemantic)
    {
        _batchIndexDate = DateTimeOffset.UtcNow;
        _markdownConverter = new HtmlToMarkdownConverter();

        // Initialize channels with site-specific index names
        // Pattern: site-lexical-{timestamp}, site-semantic-{timestamp}
    }

    public async ValueTask IndexDocumentAsync(SiteDocument document, CancellationToken ct)
    {
        // Same pattern as GuideIndexerExporter
        // Additional: populate language-specific body sub-field
    }
}
```

### 4.3 Multilingual Index Mapping

**File**: `/src/tooling/crawl-indexer/Indexing/SiteIndexMapping.cs`

```csharp
public static class SiteIndexMapping
{
    public static TypeMapping CreateMapping() => new()
    {
        Properties = new Properties
        {
            // Core fields
            ["title"] = new TextProperty { Analyzer = "standard" },
            ["search_title"] = new TextProperty { Analyzer = "standard" },
            ["url"] = new KeywordProperty(),
            ["hash"] = new KeywordProperty(),
            ["type"] = new KeywordProperty(),

            // Multilingual body with language sub-fields
            ["body"] = new TextProperty
            {
                Analyzer = "standard",
                Fields = new Properties
                {
                    ["en"] = new TextProperty { Analyzer = "english" },
                    ["de"] = new TextProperty { Analyzer = "german" },
                    ["fr"] = new TextProperty { Analyzer = "french" },
                    ["es"] = new TextProperty { Analyzer = "spanish" },
                    ["pt"] = new TextProperty { Analyzer = "portuguese" },
                    ["ja"] = new TextProperty { Analyzer = "cjk" },
                    ["ko"] = new TextProperty { Analyzer = "cjk" },
                    ["zh"] = new TextProperty { Analyzer = "cjk" }
                }
            },

            ["stripped_body"] = new TextProperty
            {
                Analyzer = "standard",
                Fields = new Properties
                {
                    ["en"] = new TextProperty { Analyzer = "english" },
                    ["de"] = new TextProperty { Analyzer = "german" },
                    ["fr"] = new TextProperty { Analyzer = "french" },
                    ["es"] = new TextProperty { Analyzer = "spanish" },
                    ["pt"] = new TextProperty { Analyzer = "portuguese" },
                    ["ja"] = new TextProperty { Analyzer = "cjk" },
                    ["ko"] = new TextProperty { Analyzer = "cjk" },
                    ["zh"] = new TextProperty { Analyzer = "cjk" }
                }
            },

            // Site-specific fields
            ["language"] = new KeywordProperty(),
            ["page_type"] = new KeywordProperty(),
            ["relevance"] = new KeywordProperty(),
            ["author"] = new KeywordProperty(),
            ["published_date"] = new DateProperty(),
            ["modified_date"] = new DateProperty(),

            // Social metadata
            ["og_title"] = new TextProperty(),
            ["og_description"] = new TextProperty(),
            ["og_image"] = new KeywordProperty(),

            // Timestamps
            ["batch_index_date"] = new DateProperty(),
            ["last_updated"] = new DateProperty(),

            // AI enrichment
            ["enrichment_key"] = new KeywordProperty(),
            ["ai_rag_optimized_summary"] = new TextProperty(),
            ["ai_short_summary"] = new TextProperty(),
            ["ai_search_query"] = new TextProperty(),
            ["ai_questions"] = new TextProperty(),
            ["ai_use_cases"] = new TextProperty()
        }
    };
}
```

### 4.4 Guide Index Mapping

**File**: `/src/tooling/crawl-indexer/Indexing/GuideIndexMapping.cs`

```csharp
public static class GuideIndexMapping
{
    public static TypeMapping CreateMapping() => new()
    {
        Properties = new Properties
        {
            // Same base fields as existing DocumentationDocument mapping
            // Plus product with version
            ["product"] = new ObjectProperty
            {
                Properties = new Properties
                {
                    ["id"] = new KeywordProperty(),
                    ["repository"] = new KeywordProperty(),
                    ["version"] = new KeywordProperty()
                }
            },

            // Navigation
            ["navigation_depth"] = new RankFeatureProperty(),
            ["parents"] = new NestedProperty
            {
                Properties = new Properties
                {
                    ["title"] = new TextProperty(),
                    ["url"] = new KeywordProperty()
                }
            }
        }
    };
}
```

### 4.5 AI Enrichment Integration

Reuse existing `ElasticsearchLlmClient` and `ElasticsearchEnrichmentCache`:

```csharp
// In exporter constructor
if (enableAiEnrichment)
{
    _llmClient = new ElasticsearchLlmClient(/* ... */);
    _enrichmentCache = new ElasticsearchEnrichmentCache(/* ... */);
}

// In IndexDocumentAsync
if (_enrichmentCache?.TryGet(enrichmentKey) is { } cached)
{
    document.AiRagOptimizedSummary = cached.RagOptimizedSummary;
    document.AiShortSummary = cached.ShortSummary;
    document.AiSearchQuery = cached.SearchQuery;
    document.AiQuestions = cached.Questions;
    document.AiUseCases = cached.UseCases;
    document.EnrichmentPromptHash = cached.PromptHash;
}
else if (_llmClient != null)
{
    var markdownBody = _markdownConverter.Convert(document.Body ?? "");
    var enrichment = await _llmClient.EnrichAsync(document.Title, markdownBody, ct);
    // Apply and cache...
}
```

### 4.6 Incremental Sync

Same pattern as existing exporter:

```csharp
public async ValueTask StopAsync(CancellationToken ct)
{
    // 1. Drain all pending writes
    await _lexicalChannel.CompleteAsync();

    // 2. Refresh to make docs searchable
    await _client.Indices.RefreshAsync(indexName, ct);

    // 3. Reindex changed docs to semantic
    if (_semanticChannel != null)
    {
        await _client.ReindexAsync(new ReindexRequest
        {
            Source = new ReindexSource
            {
                Index = lexicalIndexName,
                Query = new DateRangeQuery("last_updated")
                {
                    Gte = _batchIndexDate.ToString("o")
                }
            },
            Dest = new ReindexDestination { Index = semanticIndexName }
        }, ct);
    }

    // 4. Delete stale documents
    await _client.DeleteByQueryAsync(new DeleteByQueryRequest(lexicalIndexName)
    {
        Query = new DateRangeQuery("batch_index_date")
        {
            Lt = _batchIndexDate.ToString("o")
        }
    }, ct);
}
```

## Verification

1. Create test indices and verify mapping
2. Index sample documents and verify in Kibana
3. Test incremental sync (add, update, delete)
4. Test AI enrichment integration
5. Test multilingual search queries

## Files Created

| File | Description |
|------|-------------|
| `Indexing/GuideIndexerExporter.cs` | Guide document exporter |
| `Indexing/SiteIndexerExporter.cs` | Site document exporter |
| `Indexing/GuideIndexMapping.cs` | Guide index mapping |
| `Indexing/SiteIndexMapping.cs` | Site index mapping with multilingual |
