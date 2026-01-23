---
name: mcp-search
description: Search and analyze Elastic documentation using the remote MCP server. Use this when the user asks to search docs, find documentation, check coherence, find inconsistencies, get a document by URL, or analyze document structure. Trigger words: search, find, docs, documentation, coherence, inconsistencies, related, structure, analyze.
---

# Elastic Documentation MCP Search

This skill provides access to Elastic documentation through a remote MCP server. Use these tools to search, analyze, and verify documentation content.

## Available Tools

### 1. SemanticSearch

**When to use:** User asks to search docs, find documentation, look up information, search for topics, find examples, or search Elastic documentation.

**Trigger words:** search, find, docs, documentation, look up, examples, query

**Parameters:**
- `query` (required): The search query - can be a question or keywords
- `pageNumber` (optional, default: 1): Page number (1-based)
- `pageSize` (optional, default: 10, max: 50): Number of results per page
- `productFilter` (optional): Filter by product ID (e.g., 'elasticsearch', 'kibana', 'fleet', 'logstash')
- `sectionFilter` (optional): Filter by navigation section (e.g., 'reference', 'getting-started')

**Returns:** Search results with URLs, titles, descriptions, AI summaries, scores, and navigation context.

---

### 2. FindRelatedDocs

**When to use:** User asks to find related docs, get related content, find similar documentation, get links, or find what to link to.

**Trigger words:** related, similar, links, content reuse, related documentation, see also

**Parameters:**
- `topic` (required): Topic or search terms to find related documents for
- `limit` (optional, default: 10, max: 20): Maximum number of related documents to return
- `productFilter` (optional): Filter by product ID

**Returns:** List of related documents with URLs, titles, descriptions, AI summaries, and relevance scores.

---

### 3. CheckCoherence

**When to use:** User asks to verify document accuracy, check if content is accurate, validate documentation, check coherence, verify against docs, or ensure consistency across the documentation.

**Trigger words:** verify, check, accurate, coherent, validate, consistency, coverage, align with docs

**Parameters:**
- `topic` (required): Topic or concept to check coherence for
- `limit` (optional, default: 20, max: 50): Maximum number of documents to analyze

**Returns:** Coherence analysis including:
- Total documents covering the topic
- Section coverage (how topic is spread across navigation sections)
- Product coverage (which products document this topic)
- AI enrichment status (docs with summaries)
- Coverage score (0-100)
- Top documents for the topic

---

### 4. FindInconsistencies

**When to use:** User asks to find inconsistencies, check for contradictions, find conflicts, compare documentation, or identify discrepancies between documents.

**Trigger words:** inconsistencies, contradictions, conflicts, discrepancies, compare, differences, overlaps

**Parameters:**
- `topic` (required): Topic or concept to check for inconsistencies
- `focusArea` (optional): Specific area to focus on (e.g., 'installation', 'configuration', 'api')

**Returns:** Analysis of potential inconsistencies including:
- Documents that may have overlapping or conflicting content
- Product breakdown showing document distribution
- Pairs of documents that cover similar topics and may need review

---

### 5. GetDocumentByUrl

**When to use:** User provides a specific documentation URL or asks to get document, fetch page, retrieve doc by URL, or show a specific documentation page.

**Trigger words:** get document, fetch, URL, specific page, retrieve, show page

**Parameters:**
- `url` (required): The document URL (e.g., '/docs/elasticsearch/reference/index')
- `includeBody` (optional, default: false): Include full body content (set true for detailed analysis)

**Returns:** Full document content including:
- Title, URL, type, description
- Navigation section and parent hierarchy
- AI summaries (short summary and RAG-optimized summary)
- AI-generated questions and use cases
- Headings list
- Product and related products
- Body content (if includeBody=true)

---

### 6. AnalyzeDocumentStructure

**When to use:** User asks to analyze structure, check hierarchy, view document organization, see headings, or understand document layout.

**Trigger words:** structure, hierarchy, organization, headings, parents, layout, analyze

**Parameters:**
- `url` (required): Document URL to analyze

**Returns:** Structure analysis including:
- Heading count and list of headings
- Link count
- Parent count and parent hierarchy
- Body length
- AI enrichment status (has summary, has questions, has use cases)

---

## Usage Guidelines

1. **For general searches:** Start with `SemanticSearch` to find relevant documentation
2. **For content verification:** Use `CheckCoherence` to see how well a topic is documented
3. **For quality checks:** Use `FindInconsistencies` to identify potential documentation conflicts
4. **For specific pages:** Use `GetDocumentByUrl` when you have an exact URL
5. **For deep analysis:** Use `AnalyzeDocumentStructure` to understand page organization

## Product IDs

Common product filters:
- `elasticsearch` - Elasticsearch
- `kibana` - Kibana
- `fleet` - Fleet and Elastic Agent
- `logstash` - Logstash
- `beats` - Beats
- `cloud` - Elastic Cloud
- `ecs` - Elastic Common Schema
- `apm` - APM
- `security` - Elastic Security
- `observability` - Elastic Observability
