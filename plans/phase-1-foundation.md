# Phase 1: Foundation

## Objective
Set up project structure, extract base document class, and define new document types.

## Tasks

### 1.1 Create Project
Create `src/tooling/crawl-indexer/crawl-indexer.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <PublishAot>true</PublishAot>
    <PublishTrimmed>true</PublishTrimmed>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="AngleSharp" Version="1.x" />
    <PackageReference Include="ConsoleAppFramework" Version="x.x" />
    <PackageReference Include="Spectre.Console" Version="0.x" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../Elastic.Documentation/Elastic.Documentation.csproj" />
    <ProjectReference Include="../../Elastic.Markdown/Elastic.Markdown.csproj" />
    <ProjectReference Include="../../Elastic.Documentation.ServiceDefaults/Elastic.Documentation.ServiceDefaults.csproj" />
  </ItemGroup>
</Project>
```

### 1.2 Create Program.cs
Follow `docs-builder/Program.cs` pattern:

```csharp
var builder = Host.CreateApplicationBuilder()
    .AddDocumentationServiceDefaults(ref args)
    .AddCrawlIndexerDefaults();

var app = builder.ToConsoleAppBuilder();

app.UseFilter<ReplaceLogFilter>();
app.UseFilter<StopwatchFilter>();
app.UseFilter<CatchExceptionFilter>();

app.Add<GuideCommand>("guide");
app.Add<SiteCommand>("site");

await app.RunAsync(args);
```

### 1.3 Extract BaseSearchDocument

**File**: `/src/Elastic.Documentation/Search/BaseSearchDocument.cs`

Extract shared fields from `DocumentationDocument`:

```csharp
public abstract record BaseSearchDocument
{
    // Core fields
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("search_title")]
    public required string SearchTitle { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;

    [JsonPropertyName("batch_index_date")]
    public DateTimeOffset BatchIndexDate { get; set; }

    [JsonPropertyName("last_updated")]
    public DateTimeOffset LastUpdated { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("stripped_body")]
    public string? StrippedBody { get; set; }

    [JsonPropertyName("abstract")]
    public string? Abstract { get; set; }

    [JsonPropertyName("headings")]
    public string[] Headings { get; set; } = [];

    [JsonPropertyName("hidden")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Hidden { get; set; }

    // Social metadata
    [JsonPropertyName("og_title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OgTitle { get; set; }

    [JsonPropertyName("og_description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OgDescription { get; set; }

    [JsonPropertyName("og_image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OgImage { get; set; }

    // AI Enrichment fields (same as current DocumentationDocument)
    [JsonPropertyName("enrichment_key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EnrichmentKey { get; set; }

    [JsonPropertyName("ai_rag_optimized_summary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AiRagOptimizedSummary { get; set; }

    [JsonPropertyName("ai_short_summary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AiShortSummary { get; set; }

    [JsonPropertyName("ai_search_query")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AiSearchQuery { get; set; }

    [JsonPropertyName("ai_questions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? AiQuestions { get; set; }

    [JsonPropertyName("ai_use_cases")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? AiUseCases { get; set; }

    [JsonPropertyName("enrichment_prompt_hash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EnrichmentPromptHash { get; set; }
}
```

### 1.4 Modify DocumentationDocument

**File**: `/src/Elastic.Documentation/Search/DocumentationDocument.cs`

Change to inherit from `BaseSearchDocument`:

```csharp
public record DocumentationDocument : BaseSearchDocument
{
    // Keep doc-specific fields only
    [JsonPropertyName("product")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IndexedProduct? Product { get; set; }

    [JsonPropertyName("related_products")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IndexedProduct[]? RelatedProducts { get; set; }

    [JsonPropertyName("navigation_depth")]
    public int NavigationDepth { get; set; } = 50;

    [JsonPropertyName("navigation_table_of_contents")]
    public int NavigationTableOfContents { get; set; } = 50;

    [JsonPropertyName("navigation_section")]
    public string? NavigationSection { get; set; }

    [JsonPropertyName("applies_to")]
    public ApplicableTo? Applies { get; set; }

    [JsonPropertyName("links")]
    public string[] Links { get; set; } = [];

    [JsonPropertyName("parents")]
    public ParentDocument[] Parents { get; set; } = [];
}
```

### 1.5 Add Version to IndexedProduct

**File**: `/src/Elastic.Documentation/Search/IndexedProduct.cs`

```csharp
public record IndexedProduct
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("repository")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Repository { get; set; }

    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; set; }  // NEW
}
```

### 1.6 Create SiteDocument

**File**: `/src/Elastic.Documentation/Search/SiteDocument.cs`

```csharp
public record SiteDocument : BaseSearchDocument
{
    [JsonPropertyName("page_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PageType { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; } = "en";

    [JsonPropertyName("author")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Author { get; set; }

    [JsonPropertyName("published_date")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? PublishedDate { get; set; }

    [JsonPropertyName("modified_date")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? ModifiedDate { get; set; }

    [JsonPropertyName("relevance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Relevance { get; set; }
}
```

### 1.7 Update SourceGenerationContext

**File**: `/src/Elastic.Documentation/Serialization/SourceGenerationContext.cs`

Add new types:

```csharp
[JsonSerializable(typeof(BaseSearchDocument))]
[JsonSerializable(typeof(SiteDocument))]
```

## Verification

1. Build solution: `dotnet build`
2. Run existing tests to ensure no regression
3. Verify JSON serialization of new types

## Files Modified

| File | Change |
|------|--------|
| `/src/tooling/crawl-indexer/crawl-indexer.csproj` | NEW |
| `/src/tooling/crawl-indexer/Program.cs` | NEW |
| `/src/Elastic.Documentation/Search/BaseSearchDocument.cs` | NEW |
| `/src/Elastic.Documentation/Search/DocumentationDocument.cs` | MODIFY |
| `/src/Elastic.Documentation/Search/IndexedProduct.cs` | MODIFY |
| `/src/Elastic.Documentation/Search/SiteDocument.cs` | NEW |
| `/src/Elastic.Documentation/Serialization/SourceGenerationContext.cs` | MODIFY |
