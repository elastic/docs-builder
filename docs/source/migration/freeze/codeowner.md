---
title: Codeowner
---

This document lists the CODEOWNERS configuration used to enforce documentation freezes across Elastic repositories. During a documentation freeze, the `@docs-freeze-team` must approve any pull requests that modify documentation files (`.asciidoc`) before they can be merged. This prevents unintended documentation changes during crucial periods such as releases.

To regenerate this list, run the following commands from the repository root:

```sh
cd docs/source/migration/freeze/scripts/codeowner-gen
python3 codeowner.py
```

This will process `conf.yaml` and generate an updated `output.md` file containing the CODEOWNERS configuration for all repositories with documentation. The output is automatically included in this README. Note that the `conf.yaml` include in this repo should be replaced by a current version before running this script again.

## Documentation source repositories

:::{include} scripts/codeowner-gen/output.md
:::