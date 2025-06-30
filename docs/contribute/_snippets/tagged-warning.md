:::{warning}
Some repositories use a [tagged branching strategy](/contribute/branching-strategy.md), which means that their docs are published from a branch that is not `main` or `master`. In these cases, documentation changes need to be made to `main` or `master`, and then backported to the relevant branches.

For detailed backporting guidance, refer to the example in [Choose the docs branching strategy for a repository](/contribute/branching-strategy.md#workflow-2-tagged-deployment).

To determine the published branches for a repository, find the repository in [assembler.yml](https://github.com/elastic/docs-builder/blob/main/src/tooling/docs-assembler/assembler.yml).
:::
