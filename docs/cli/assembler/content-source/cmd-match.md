## Description

Checks whether a repository at a specific branch or tag should be included in the next build. Emits the following `$GITHUB_OUTPUT` variables:

* `content-source-match` — whether the branch is a configured content source.
* `content-source-next` — whether the branch is the next content source.
* `content-source-current` — whether the branch is the current content source.
* `content-source-speculative` — whether the branch is a speculative content source.
* `content-source-ref` — the branch that matched. Equal to the requested branch unless a stacked pull request was resolved to its root branch.
* `stack-parent-prs` — comma-separated numbers of the open pull requests walked through to reach `content-source-ref`, nearest parent first. Empty when the branch is not stacked.

## Stacked pull requests

A stacked pull request targets the head branch of another open pull request instead of a content-source branch. When the requested branch is not a content source and a GitHub token is available, the command follows the chain of open pull requests from that branch until it reaches a content-source branch. Each hop is tested against the real content-source configuration, so a branch that is itself a content source is never walked past. A pull request that targets `main` is matched directly, even if another open pull request uses `main` as its head.

The walk stops without a match when a branch is not the head of any open pull request, when more than one open pull request shares the same head branch, when the chain loops, or when the chain is deeper than 50 pull requests.

The token comes from the `github_token` action input or the `GITHUB_TOKEN` environment variable. It needs read access to pull requests. Without a token, stacked pull requests are not resolved and the command behaves as before.

## Speculative builds

If branches follow semantic versioning and a branch is cut that is greater than the current version, it is considered a speculative build.
`docs-builder`'s shared workflow triggers even if the branch is not yet specified as a content source in `assembler.yml`.

This allows a branch's `links.json` to be published to the Links Service ahead of time, before the branch is officially configured as a content source.
