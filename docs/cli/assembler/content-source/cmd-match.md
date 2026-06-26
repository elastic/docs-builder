## Description

Checks whether a repository at a specific branch or tag should be included in the next build. Emits the following `$GITHUB_OUTPUT` variables:

* `content-source-match` — whether the branch is a configured content source.
* `content-source-next` — whether the branch is the next content source.
* `content-source-current` — whether the branch is the current content source.
* `content-source-speculative` — whether the branch is a speculative content source.

## Speculative builds

If branches follow semantic versioning and a branch is cut that is greater than the current version, it is considered a speculative build.
`docs-builder`'s shared workflow triggers even if the branch is not yet specified as a content source in `assembler.yml`.

This allows a branch's `links.json` to be published to the Links Service ahead of time, before the branch is officially configured as a content source.
