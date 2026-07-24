# Backfill artifact contracts

The changelog backfill pipeline (epic [elastic/docs-eng-team#656](https://github.com/elastic/docs-eng-team/issues/656))
runs as a series of stages, and each stage hands its result to the next one as a JSON document.
The types in this folder define what those documents look like, so every stage — and every human
reviewing a run — reads and writes the same shapes.

## The six document families

| Family | What it is | Who writes it | Who reads it |
|---|---|---|---|
| **inventory** | The census: which products and release-note sources exist, where they came from, and what we decided about each. | The inventory stage | Planning; humans reviewing scope |
| **overrides** | Manual corrections an operator feeds into planning, each with a reason attached. | A human operator | Planning |
| **semantic-model** | The release notes reduced to their meaning, with formatting stripped away. | The parser | Planning; the fidelity gate |
| **plan** | Exactly what we intend to create in S3, pinned to all of its inputs. | Planning | A human approver; the apply stage |
| **provenance** | The evidence trail: why we believe each recovered fact (an entry's type, a release date, …). | The parser and enrichment | Humans reviewing a scope |
| **ledger** | What actually happened when a plan was applied: every attempted step and its outcome. | The apply stage | Reruns (to resume safely); auditors |

## How they flow

1. The inventory stage takes the census and writes an **inventory**; a human adds **overrides** where the census got something wrong.
2. The parser turns release-note sources into a **semantic-model** (plus **provenance** for every recovered fact).
3. Planning combines inventory + overrides + semantic model + the current S3 state into a **plan**.
4. A human approves the plan; the apply stage executes it and writes a **ledger** of what really happened.
5. If a run is interrupted, the next run reads the ledger and the (unchanged, content-addressed) plan and picks up where it left off.

## Envelopes and versions

Every persisted document is wrapped in a small envelope that records what kind of document the
file contains (`artifact`) and which schema version wrote it (`schema_version`). Readers check
both **before** parsing the payload and fail with a clear error on anything they don't
understand — there is no silent best-effort parsing. All six families are currently at
version 1; bump a family's version in `BackfillSchemaVersions` when its shape changes in a way
old readers cannot safely ignore.

## Canonical form and hashing

Documents are hashed so a plan can be pinned to the exact inputs it was computed from, and so
the same plan content always has the same identity ("content-addressed"). Because the same JSON
can be written many equivalent ways, hashing first rewrites the document into one agreed-upon
**canonical form**:

- object keys sorted by ordinal (byte-order) comparison;
- no insignificant whitespace;
- `\r\n` and `\r` inside strings normalized to `\n`;
- properties whose value is null are omitted entirely (absent and null mean the same thing);
- timestamps are stored in UTC and dates as `yyyy-MM-dd`, so the text never depends on machine culture or timezone;
- array order is preserved — it is part of the meaning; dictionaries become sorted JSON objects, so insertion order never matters.

The hash is SHA-256 over the UTF-8 bytes of the canonical JSON of the **whole envelope**
(so the schema version is covered too), written as `sha256:` + 64 lower-case hex characters.
The pretty-printed form on disk is irrelevant to the hash — reading a file back and re-hashing
it always gives the same answer.

Use `BackfillDocuments` to read, write, and hash documents; it applies all of the above.
