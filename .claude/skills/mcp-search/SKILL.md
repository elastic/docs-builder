---
name: mcp-search
description: Search, analyze, and author Elastic documentation using the remote MCP server. Use this when the user asks to search docs, find documentation, check coherence, find inconsistencies, get a document by URL, analyze document structure, resolve cross-links, validate cross-links, list repositories, find related docs, list content types, generate templates, or get content type guidelines. Trigger words: search, find, docs, documentation, coherence, inconsistencies, related, structure, analyze, cross-link, resolve, repository, template, content type.
---

# Elastic Documentation MCP

This skill provides access to Elastic documentation through a remote MCP server. Use these tools to search, analyze, author, and validate documentation content.

## Available Tools

### 1. SemanticSearch

**When to use:** User asks to search docs, find documentation, look up information, search for topics, find examples, or search Elastic documentation.

**Trigger words:** search, find, docs, documentation, look up, examples, query

**Parameters:**
- `query` (required): The search query — can be a question or keywords
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
- `url` (required): The document URL. Accepts a full URL (e.g., `https://www.elastic.co/docs/deploy-manage/api-keys`) or a path (e.g., `/docs/deploy-manage/api-keys`). Query strings, fragments, and trailing slashes are ignored.
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
- `url` (required): The document URL to analyze. Accepts a full URL (e.g., `https://www.elastic.co/docs/deploy-manage/api-keys`) or a path (e.g., `/docs/deploy-manage/api-keys`). Query strings, fragments, and trailing slashes are ignored.

**Returns:** Structure analysis including:
- Heading count and list of headings
- Link count
- Parent count and parent hierarchy
- Body length
- AI enrichment status (has summary, has questions, has use cases)

---

### 7. ResolveCrossLink

**When to use:** User provides a cross-link URI (e.g., `docs-content://get-started/intro.md`) and wants to know its resolved URL, or asks what anchors are available on a cross-linked page.

**Trigger words:** cross-link, resolve, cross link, docs-content://, URI, anchor

**Parameters:**
- `crossLink` (required): The cross-link URI to resolve (e.g., `docs-content://get-started/intro.md`)

**Returns:** Resolved URL, repository, path, available anchors, and any fragment.

---

### 8. ListRepositories

**When to use:** User wants to know which repositories are available in the cross-link index, or asks what repos can be linked to.

**Trigger words:** list repos, repositories, available repos, cross-link index

**Parameters:** None

**Returns:** List of repositories with names, branches, paths, git refs, and last-updated timestamps.

---

### 9. GetRepositoryLinks

**When to use:** User wants to see all pages and anchors published by a specific repository, or is auditing what a repo exposes for cross-linking.

**Trigger words:** repository links, pages in repo, anchors, what does repo publish

**Parameters:**
- `repository` (required): The repository name (e.g., `docs-content`, `elasticsearch`)

**Returns:** Repository metadata, URL path prefix, page count, cross-link count, and a list of pages with their anchors.

---

### 10. FindCrossLinks

**When to use:** User wants to find all cross-links between repositories, either from a specific source repo or pointing to a specific target repo.

**Trigger words:** find cross-links, links between repos, who links to, links from

**Parameters:**
- `from` (optional): Source repository to find links from
- `to` (optional): Target repository to find links to

**Returns:** Count and list of cross-links with source repository, target repository, and the link URI.

---

### 11. ValidateCrossLinks

**When to use:** User wants to validate cross-links pointing to a repository and find any that are broken.

**Trigger words:** validate cross-links, broken links, check links, link validation

**Parameters:**
- `repository` (required): Target repository to validate links to (e.g., `docs-content`)

**Returns:** Valid link count, broken link count, and details of any broken links (source repo, link URI, errors).

---

### 12. ListContentTypes

**When to use:** User wants to know what content types are available, is deciding what type of page to write, or asks for guidance on content type selection.

**Trigger words:** content types, what type, overview vs how-to, tutorial, troubleshooting, changelog type

**Parameters:** None

**Returns:** List of all content types (overview, how-to, tutorial, troubleshooting, changelog) with descriptions and when-to-use guidance.

---

### 13. GenerateTemplate

**When to use:** User wants a starting template for a new documentation page or changelog entry.

**Trigger words:** template, generate, starter, scaffold, new page, new doc

**Parameters:**
- `contentType` (required): One of `overview`, `how-to`, `tutorial`, `troubleshooting`, or `changelog`
- `title` (optional): Pre-fill the page or changelog title
- `description` (optional): Pre-fill the frontmatter description
- `product` (optional): Pre-fill the product field

**Returns:** A ready-to-use Markdown template (or YAML for changelogs) with correct frontmatter and structure.

---

### 14. GetContentTypeGuidelines

**When to use:** User wants detailed authoring guidance for a content type, is evaluating existing content against standards, or asks about best practices for a content type.

**Trigger words:** guidelines, best practices, how to write, checklist, evaluate, anti-patterns

**Parameters:**
- `contentType` (required): One of `overview`, `how-to`, `tutorial`, `troubleshooting`, or `changelog`

**Returns:** Detailed guidelines including required elements checklist, recommended sections, best practices, and anti-patterns for the content type.

---

## Usage Guidelines

1. **For general searches:** Start with `SemanticSearch` to find relevant documentation.
2. **For content verification:** Use `CheckCoherence` to see how well a topic is documented.
3. **For quality checks:** Use `FindInconsistencies` to identify potential documentation conflicts.
4. **For specific pages:** Use `GetDocumentByUrl` when you have an exact URL.
5. **For deep analysis:** Use `AnalyzeDocumentStructure` to understand page organization.
6. **For cross-linking:** Use `ResolveCrossLink` to turn a cross-link URI into a real URL, `ListRepositories` to discover available repos, and `ValidateCrossLinks` to find broken links.
7. **For authoring:** Use `ListContentTypes` to pick the right type, `GenerateTemplate` to get a starter, and `GetContentTypeGuidelines` to write or evaluate content correctly.

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
