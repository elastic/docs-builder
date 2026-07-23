---
navigation_title: Page feedback
---

# Page feedback

The documentation site records page-level reactions and optional comments in the
`page-feedback-v1-{environment}` Elasticsearch index. The API uses the feedback
identifier as the document `_id`, so later comment submissions and reaction
changes replace the same document.

## Provision the index

`PageFeedbackMapping` defines the mapping with `Elastic.Mapping` attributes.
During API startup, `PageFeedbackBootstrapService` uses
`Elastic.Ingest.Elasticsearch` to create or update environment-specific
component and index templates. Template updates are skipped when the generated
mapping hash has not changed. The first feedback write creates the concrete
index from those templates.

The generated mapping disables dynamic field mapping. Fields that are not part
of `PageFeedbackDocument` remain unindexed instead of changing the schema.

Bootstrap failures are logged and ignored in `dev` so the local documentation
server can run without Elasticsearch. They prevent startup in `staging`, `edge`,
and `prod`.

Runtime credentials require permission to manage component and index templates,
create the environment's index, and write and delete its documents.

Template updates do not modify existing indices. For a mapping change, increment
the schema version in the index name, deploy the new template, and migrate any
documents that must be retained. Mixed task versions then write to separate
versioned indices during a rolling deployment.
