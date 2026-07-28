## Description

Sends one explicit reconcile message per planned group to the scrubber queue. For each message the Lambda performs a **full group heal**: an object-level reconcile over the union of both buckets' listings (scrub and copy whatever is live in the private bucket, delete public objects nothing backs), then a rebuild of the group's public `registry.json` from the public listing. This recovers drift that no pending S3 event would ever repair — lost or DLQ-expired scrub events, ad-hoc uploads, orphaned public objects.

Without a scope filter, the plan is the union of groups discovered in **both** buckets, so orphan public groups (including groups that only have a leftover manifest) are covered. Use `--product`, or `--owner`/`--repo`/`--branch` together, to reconcile a single group.

The command is convergent: re-running it re-plans against current state and the Lambda's writes are conditional, so overlapping runs cannot corrupt a manifest. It is also deliberately indirect — the CLI only sends queue messages, keeping the scrubber Lambda the sole writer of the public bucket.

Every run stamps one correlation id on all its messages and prints a ledger line per group (`group`, SQS `message-id`, `correlation-id`). **Enqueuing is not reconciling**: after a run, watch the queue drain (oldest-message-age ≈ 0), triage anything that reaches the DLQ, and gate on [](/cli/changelog/registry/verify.md) reporting zero divergence.

## Examples

Preview the full plan without sending anything:

```bash
docs-builder changelog registry reconcile \
  --s3-bucket-name elastic-docs-v3-changelog-bundles-private \
  --public-s3-bucket-name elastic-docs-v3-changelog-bundles \
  --queue-url https://sqs.us-east-1.amazonaws.com/<account>/elastic-docs-v3-changelog-scrub-queue \
  --dry-run
```

Reconcile a single product's bundle registry non-interactively:

```bash
docs-builder changelog registry reconcile \
  --s3-bucket-name elastic-docs-v3-changelog-bundles-private \
  --public-s3-bucket-name elastic-docs-v3-changelog-bundles \
  --queue-url https://sqs.us-east-1.amazonaws.com/<account>/elastic-docs-v3-changelog-scrub-queue \
  --product elasticsearch --yes
```
