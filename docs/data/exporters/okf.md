---
navigation_title: OKF
---

# Open Knowledge Format (OKF) exporter

The OKF exporter produces a zip bundle conforming to the [Open Knowledge Format v0.1 specification](https://github.com/GoogleCloudPlatform/knowledge-catalog/blob/main/okf/SPEC.md), a structured format for packaging knowledge bases.

## Output

The exporter generates an `okf.zip` bundle containing:

- **Concept files** — each documentation page as a Markdown file with links rewritten to bundle-relative paths
- **Directory index files** — synthesized navigation indexes for each folder in the documentation tree
- **Metadata** — structured frontmatter preserved from source pages

## Key differences from LLM export

| | OKF | LLM Markdown |
|---|---|---|
| **Links** | Bundle-relative paths | Public URLs |
| **Format** | Zip bundle | Individual files + `llms.txt` index |
| **Index pages** | Synthesized directory listings | Authored landing pages preserved |
| **Spec** | OKF v0.1 | llms.txt |

## Use cases

- Knowledge base interchange between platforms
- Offline documentation packages
- Structured content import into knowledge management systems
