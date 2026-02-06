# Overview content type guidelines

Source: https://www.elastic.co/docs/contribute-docs/content-types/overviews

## What is an overview

An overview provides conceptual information that helps users understand a feature, product, or concept. It answers three fundamental questions: What is it? How does it work? How does it bring value?

Overviews serve to:

- Explain what something is and why it matters.
- Inform users how features and capabilities improve their workflows.
- Help users navigate to the right content for their needs.
- Clarify how components, features, or concepts relate to each other.
- Help users choose between options or understand trade-offs.

## Required elements checklist

- [ ] **Filename:** Descriptive noun-based pattern (e.g., `text-embedding.md`, `data-streams.md`, `index.md` for landing pages).
- [ ] **Frontmatter `applies_to`:** Tags for versioning/availability info.
- [ ] **Frontmatter `description`:** Brief summary fit for search results and tooltips.
- [ ] **Frontmatter `product`:** Relevant Elastic product(s).
- [ ] **Title:** Concise, descriptive name for the feature or concept (e.g., "Text embedding", "Data streams").
- [ ] **Introduction:** Explains what the feature/concept is and why it matters. Answers "What is it?" at a high level. Establishes scope.
- [ ] **Core content sections:** Body explaining how it works, key concepts, components, or use cases.

## Recommended sections checklist

- [ ] **Use cases or examples:** Concrete scenarios showing how the feature applies in practice.
- [ ] **How it works:** Underlying mechanism, workflow, or architecture. Consider including a diagram.
- [ ] **Next steps:** Getting started guides, tutorials, or how-to guides.
- [ ] **Related pages:** Links to how-to guides, reference material, or other overviews.

## Optional elements

- Background or context (historical, industry, or design rationale).
- Diagrams (architecture, flowcharts, visual aids).
- Comparison tables (for choosing between options).
- Tabs (for deployment type or product tier variations).
- Key terminology definitions.

## Best practices

- Focus on a single concept per overview.
- Lead with user value, not just what it does.
- Use the inverted pyramid: most important information first, then progressive detail.
- Keep it conceptual: no step-by-step instructions. Link to how-to guides instead.
- Answer the key questions: What is it? Why does it matter? How does it work? When would I use it?
- Use concrete examples and real-world scenarios.
- Provide visual aids for complex relationships.
- Avoid duplication: link to reference or how-to pages for detailed info.

## Anti-patterns to avoid

- Embedding step-by-step instructions (use how-to guides for that).
- Explaining multiple concepts in depth on one page (split into separate overviews).
- Duplicating detailed reference or how-to content.
- Starting with technical details before establishing user value.
