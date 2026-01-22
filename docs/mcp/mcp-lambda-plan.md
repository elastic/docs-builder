# Remote MCP Server Lambda Implementation Plan

## Overview

1. Create a new shared Search service at `src/services/Elastic.Documentation.Search/`
2. Move search code from `Api.Infrastructure/Adapters/Search/` to the new service
3. Create MCP Lambda at `src/api/Elastic.Documentation.Mcp.Lambda/`
4. Update existing `Api.Lambda` to reference the new Search service

## Part 1: New Search Service Project

### Location
`src/services/Elastic.Documentation.Search/`

### Files to Move from Api.Infrastructure/Adapters/Search/
```
src/services/Elastic.Documentation.Search/
├── Elastic.Documentation.Search.csproj
├── FullSearchGateway.cs           # from Api.Infrastructure
├── NavigationSearchGateway.cs     # from Api.Infrastructure
├── MockSearchGateway.cs           # from Api.Infrastructure
├── ElasticsearchOptions.cs        # from Api.Infrastructure
├── Common/
│   ├── ElasticsearchClientAccessor.cs
│   ├── SearchQueryBuilder.cs
│   └── SearchResultProcessor.cs
├── StringHighlightExtensions.cs
└── ServicesExtension.cs           # DI registration for search services
```

### Dependencies
- `Elastic.Documentation.Api.Core` (for IFullSearchGateway, models)
- `Elastic.Clients.Elasticsearch`
- `Microsoft.Extensions.DependencyInjection.Abstractions`

---

## Part 2: MCP Lambda Project

### Location
`src/api/Elastic.Documentation.Mcp.Lambda/`

### Structure
```
src/api/Elastic.Documentation.Mcp.Lambda/
├── Elastic.Documentation.Mcp.Lambda.csproj
├── Program.cs
├── Dockerfile
├── appsettings.json
├── appsettings.development.json
├── appsettings.edge.json
├── Properties/
│   └── launchSettings.json
├── Tools/
│   ├── SearchTools.cs           # semantic_search, find_related_docs
│   ├── CoherenceTools.cs        # check_coherence, find_inconsistencies
│   └── DocumentTools.cs         # get_document_by_url, analyze_document_structure
├── Gateways/
│   ├── IDocumentGateway.cs      # MCP-specific document operations
│   └── DocumentGateway.cs
└── Responses/
    └── McpResponses.cs          # Source-generated JSON context
```

### Dependencies

#### New Package (add to Directory.Packages.props)
```xml
<PackageVersion Include="ModelContextProtocol.AspNetCore" Version="0.2.0-preview.1" />
```

#### Project References
- `Elastic.Documentation.ServiceDefaults`
- `Elastic.Documentation.Api.Core` (for interfaces and models)
- `Elastic.Documentation.Search` (new shared search service)

#### Package References
- `Amazon.Lambda.AspNetCoreServer.Hosting`
- `ModelContextProtocol.AspNetCore`
- `Elastic.OpenTelemetry`
- `OpenTelemetry.Instrumentation.AspNetCore`

---

## Part 3: Update Existing Api.Lambda

### Changes Required
- Remove project reference to `Api.Infrastructure` search components
- Add project reference to `Elastic.Documentation.Search`
- Update `ServicesExtension.cs` to use search service DI registration

---

## Tool Implementation Mapping

| POC Tool | .NET Implementation | Gateway |
|----------|---------------------|---------|
| `semantic_search` | `SearchTools.SemanticSearch()` | `IFullSearchGateway` (from Search service) |
| `find_related_docs` | `SearchTools.FindRelatedDocs()` | `IFullSearchGateway` (from Search service) |
| `check_coherence` | `CoherenceTools.CheckCoherence()` | `IFullSearchGateway` (from Search service) |
| `find_inconsistencies` | `CoherenceTools.FindInconsistencies()` | `IFullSearchGateway` (from Search service) |
| `get_document_by_url` | `DocumentTools.GetDocumentByUrl()` | `IDocumentGateway` (new, in MCP Lambda) |
| `analyze_document_structure` | `DocumentTools.AnalyzeStructure()` | `IDocumentGateway` (new, in MCP Lambda) |

## Key Implementation Details

### Program.cs Pattern
- Use `WebApplication.CreateSlimBuilder()` for minimal APIs
- Support both Lambda Web Adapter (LWA) and API Gateway modes
- Configure MCP with HTTP transport: `.WithHttpTransport()`
- Auto-discover tools: `.WithToolsFromAssembly()`
- Map MCP endpoint: `app.MapMcp()`

### New Gateway: IDocumentGateway
```csharp
public interface IDocumentGateway
{
    Task<DocumentResult?> GetByUrlAsync(string url, CancellationToken ct = default);
    Task<DocumentStructure?> GetStructureAsync(string url, CancellationToken ct = default);
}
```

Implementation uses `ElasticsearchClientAccessor` with term query on `url.keyword`.

### AOT Compatibility
- All response types need `[JsonSerializable]` attributes
- Use source-generated JSON serialization context
- Follow pattern from existing `McpJsonContext` in `Responses.cs`

## Build Infrastructure

### Files to Create
1. `src/api/Elastic.Documentation.Mcp.Lambda/Dockerfile` - based on existing Lambda Dockerfile
2. `.github/workflows/build-mcp-lambda.yml` - reusable build workflow
3. `.github/workflows/deploy-mcp-lambda.yml` - reusable deploy workflow
4. `.github/workflows/deploy-mcp-lambda-edge.yml` - auto-deploy on main push

### AWS Resources Needed
- Lambda function: `elastic-docs-v3-mcp-{environment}`
- S3 bucket: `elastic-docs-v3-mcp-lambda-artifacts`
- IAM role: `elastic-docs-v3-mcp-deployer-{environment}`
- Lambda Function URL or API Gateway for HTTP access

## Environment Variables
```
ENVIRONMENT=edge|staging|prod
DOCUMENTATION_ELASTIC_URL=<elasticsearch-url>
DOCUMENTATION_ELASTIC_APIKEY=<api-key>
DOCUMENTATION_ELASTIC_INDEX=semantic-docs-{env}-latest
```

## Verification Steps

1. **Build verification**: `dotnet build` for all affected projects
2. **Docker build**: `docker build -f src/api/Elastic.Documentation.Mcp.Lambda/Dockerfile .`
3. **Local testing**: Run with `dotnet run` and test via MCP Inspector
4. **Tool testing**: Verify each tool returns expected results via HTTP POST to `/mcp`

## Implementation Order

### Phase 1: Create Search Service (refactoring)
1. Create `src/services/Elastic.Documentation.Search/Elastic.Documentation.Search.csproj`
2. Move files from `Api.Infrastructure/Adapters/Search/` to new project
3. Update namespaces from `Elastic.Documentation.Api.Infrastructure.Adapters.Search` to `Elastic.Documentation.Search`
4. Create `ServicesExtension.cs` in Search service for DI registration
5. Update `Api.Infrastructure` to remove moved files and add reference to Search service
6. Update `Api.Lambda` to reference Search service
7. Verify existing Api.Lambda still builds and works

### Phase 2: Create MCP Lambda Project
8. Create `src/api/Elastic.Documentation.Mcp.Lambda/Elastic.Documentation.Mcp.Lambda.csproj`
9. Implement `Program.cs` with MCP HTTP transport
10. Create `IDocumentGateway` and `DocumentGateway` for document-specific operations
11. Implement `SearchTools.cs` (semantic_search, find_related_docs)
12. Implement `CoherenceTools.cs` (check_coherence, find_inconsistencies)
13. Implement `DocumentTools.cs` (get_document_by_url, analyze_document_structure)
14. Add `McpResponses.cs` with source-generated JSON serialization
15. Create appsettings files and launchSettings.json

### Phase 3: Build Infrastructure
16. Create `Dockerfile` for MCP Lambda
17. Create `.github/workflows/build-mcp-lambda.yml`
18. Create `.github/workflows/deploy-mcp-lambda.yml`
19. Add MCP Lambda build to `ci.yml`

### Phase 4: Local Testing
20. Verify all projects build: `dotnet build`
21. Test locally with `dotnet run`
22. Test with MCP Inspector: `npx @modelcontextprotocol/inspector`

---

## Critical Files Reference

### Files to Create
- `src/services/Elastic.Documentation.Search/Elastic.Documentation.Search.csproj`
- `src/services/Elastic.Documentation.Search/ServicesExtension.cs`
- `src/api/Elastic.Documentation.Mcp.Lambda/Elastic.Documentation.Mcp.Lambda.csproj`
- `src/api/Elastic.Documentation.Mcp.Lambda/Program.cs`
- `src/api/Elastic.Documentation.Mcp.Lambda/Dockerfile`
- `src/api/Elastic.Documentation.Mcp.Lambda/Tools/SearchTools.cs`
- `src/api/Elastic.Documentation.Mcp.Lambda/Tools/CoherenceTools.cs`
- `src/api/Elastic.Documentation.Mcp.Lambda/Tools/DocumentTools.cs`
- `src/api/Elastic.Documentation.Mcp.Lambda/Gateways/IDocumentGateway.cs`
- `src/api/Elastic.Documentation.Mcp.Lambda/Gateways/DocumentGateway.cs`
- `src/api/Elastic.Documentation.Mcp.Lambda/Responses/McpResponses.cs`
- `.github/workflows/build-mcp-lambda.yml`
- `.github/workflows/deploy-mcp-lambda.yml`

### Files to Move (refactor)
- `src/api/Elastic.Documentation.Api.Infrastructure/Adapters/Search/*.cs` → `src/services/Elastic.Documentation.Search/`

### Files to Modify
- `Directory.Packages.props` - add ModelContextProtocol.AspNetCore
- `src/api/Elastic.Documentation.Api.Infrastructure/ServicesExtension.cs` - remove search registration
- `src/api/Elastic.Documentation.Api.Infrastructure/Elastic.Documentation.Api.Infrastructure.csproj` - update refs
- `.github/workflows/ci.yml` - add MCP Lambda build

### Reference Files (patterns to follow)
- `src/api/Elastic.Documentation.Api.Lambda/Program.cs` - Lambda hosting pattern
- `src/api/Elastic.Documentation.Api.Lambda/Dockerfile` - Docker build pattern
- `src/Elastic.Documentation.Mcp/LinkTools.cs` - MCP tool implementation pattern
- `src/Elastic.Documentation.Mcp/Responses.cs` - JSON serialization context pattern
- `.github/workflows/build-api-lambda.yml` - Build workflow pattern
