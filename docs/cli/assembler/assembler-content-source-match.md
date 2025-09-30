---
navigation_title: "content-source match"
---

# assembler content-source match

This command is used to match a repository and branch to a content source it will emit the following `$GITHUB_OUTPUT`:

* `content-source-match` - whether the branch is a configured content source
* `content-source-next` - whether the branch is the next content source
* `content-source-current` - whether the branch is the current content source
* `content-source-speculative` - whether the branch is a speculative content source.

#### Speculative builds

If branches follow semantic versioning, if a branch is cut that is greater than the current version, it will be considered a speculative build.
`docs-builer`'s shared workflow will trigger even if it's not specified as a content source in `assembler.yml`.

This allows a branch `links.json` to be published to the `Link Service` a head of time before it's configured as a content source.

## Usage

```
docs-builder assembler content-source match <repository> <branch> [-h|--help] [--version]
```

## Arguments

`<repository>
: The name of the `elastic/<repository>` repository you want to match if it should be build on CI

`<branch>
: The branch you want to match if it should be build on CI`