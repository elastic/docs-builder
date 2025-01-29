# Quickstart

## Download the docs-builder binary

TBD

## Set up folder structure

```
root/
├── docs/
│   └── index.md
│   └── docset.yml
└── ...
```




## Set up preview environment

:::{dropdown} .github/workflows/docs.yml
:open:
```markdown
name: docs

on:
  pull_request_target:
    types:
      - synchronize
      - opened
      - reopened

permissions:
  contents: read

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: ${{ github.ref != 'refs/heads/main' }}

jobs:
  docs-preview:
    uses: elastic/docs-builder/.github/workflows/preview.yml@feature/consumer-preview-action
    permissions:
      contents: read
      id-token: write
      deployments: write
    with:
      strict: false
```

:::

:::{dropdown} .github/workflows/docs-cleanup.yml
:open:
```markdown
name: docs

on:
  pull_request_target:
    types:
      - closed

jobs:
  docs-preview:
    uses: elastic/docs-builder/.github/workflows/preview-cleanup.yml@main
    permissions:
      contents: read
      id-token: write
      deployments: write
```

:::
