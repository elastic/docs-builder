---
navigation_title: Page feedback
---

# Page feedback

The documentation site records page-level reactions and optional comments in the
`page-feedback-v1-{environment}` Elasticsearch index. The API uses the feedback
identifier as the document `_id`, so later comment submissions and reaction
changes replace the same document.

The thumbs-up or thumbs-down selection writes a document with the reaction after
a short debounce, so quickly changing the selection records only the final
choice. The follow-up questionnaire writes the same document again with a
structured reason, the reason-set version, and optional details. Submitting the
questionnaire flushes any pending reaction write first. This keeps abandoned
questionnaires useful while ensuring the richer submission wins.

Reasons are stored as keyword enum values for filtering and aggregation.
`reason_set_version` identifies the questionnaire revision that presented the
option. Display labels may change without changing their stored value. Add a new
enum value when an option's meaning changes, and retain retired values so older
clients and historical documents remain valid.

The current positive reasons are `accurate`, `solvedProblem`,
`easyToUnderstand`, `helpfulExamples`, and `anotherReason`. The current negative
reasons are `inaccurate`, `missingInformation`, `hardToUnderstand`,
`codeSampleErrors`, and `anotherReason`.

## Provision the index

`PageFeedbackMapping` defines the mapping with `Elastic.Mapping` attributes.
During API startup, `PageFeedbackBootstrapService` uses
`Elastic.Ingest.Elasticsearch` to create or update environment-specific
component and index templates. The generated context uses the API assembly
version as its mapping version. Bootstrap skips unchanged mapping hashes and
prevents an older API task from replacing templates installed by a newer task
during a rolling deployment.

The bootstrap service and runtime gateway share one ingest channel. Feedback
upserts use `DirectWriteAsync` and await the bulk item result before the API
responds. They do not use the channel's retry overload because the browser owns
the bounded retry UX. The first successful feedback write creates the concrete
index from the templates.

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
versioned indices during a rolling deployment. The assembly mapping version is
an additional downgrade guard for templates that retain the same schema-versioned
name; it does not replace index versioning.
