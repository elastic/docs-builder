---
navigation_title: Choose a branching strategy
---

# Choose the docs branching strategy for a repository

With Docs V3 (elastic.co/docs), a single branch is published per repository. This branch is set to `main` by default. This is known as the continuous deployment branching strategy. However, it is possible to instead publish a different branch, also known as the tagged branching strategy. 

On this page, you'll learn how to choose the right branching strategy for your repository, and how to change the branching strategy. You'll also learn about the workflows for working with each branching strategy.

## Why is `main` the default publication branch?

The main reasons for this choice are:

* With Docs V3, there is no longer a different version of each page for each minor release. Instead, the same page [covers all versions](cumulative-docs.md), and any changes are indicated throughout the content.  
* More and more products are released from `main` branches, making these branches the most up to date at any given time. This is especially true for {{serverless-full}} and {{ecloud}}.

 
## Why would we want to publish a different branch instead?

Publishing from the main branch isn’t the best option for all repositories.

* `main` can contain code and docs for unreleased versions that we don’t want to publish yet.  
* The versioning scheme and release cadence of the product associated with a repository can vary, and it can be inconsistent to have the docs associated with a certain version live in a different branch than the code.

If you choose this publication model for your repository AND that repository includes {{serverless-short}} or {{ecloud}} documentation, you will need to make sure that {{serverless-short}}- and {{ecloud}}-related changes are also backported to the `current` branch in order to be published on time.

You **don't** need to change your branching strategy to enable writing docs about future versions. Review the [continuous deployment workflow](#workflow-1-default-continuous-deployment) and [](cumulative-docs.md) to learn more.

Note that regardless of the publication branch that is set, the documentation must still flag all changes introduced so far since the last major release. This is NOT an alternative to [writing docs cumulatively](cumulative-docs.md).

## How to change the published branch

Choosing to switch between publishing docs from `main` and publishing docs from a version branch is a long-term decision. This decision impacts all docs for an entire repository. Reach out to the docs team to discuss the change.

For more information, refer to [](/configure/content-sources.md).

After it has been established that a repository should publish from a version branch rather than `main`:

1. [Add new triggers to the `docs-build` CI integration](/configure/content-sources.md#ci-configuration). Merge these changes to `main` or `master` and the intended version branches.
2. Open a PR to trigger the CI integration and confirm that the docs build.
3. Open a PR updating the [docs assembler file](https://github.com/elastic/docs-builder/blob/main/src/tooling/docs-assembler/assembler.yml):  
   * Specify which is the `current` branch for the repository. This branch is the branch from which docs are deployed to production at [elastic.co/docs](http://elastic.co/docs).  
   * Specify which is the `next` branch for the repository. The branch defined as `next` publishes docs internally to [staging-website.elastic.co/docs](http://staging-website.elastic.co/docs)  
     * Setting this branch to the next version branch in line is a good practice to preview docs change for an upcoming version.  
     * Otherwise, keeping it set to `main` is also an option since this is where the content is initially developed and merged. This is the default.
4. In the assembler PR, add the `ci` label. After CI runs, confirm that the intended version branches are publishing to the link service. When links are being published as intended, they can be found at the following URL, where `repo` is your repo name and `branch` is your newly configured branch:

  ```text
  elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/<repo>/<branch>/links.json
  ```
5. Rerun the `validate-assembler` check on the PR.
6. After checks pass and the docs engineering team approves, you can merge the PR.

After these steps are completed, the docs engineering team needs to release a new version of our build tool to complete the process. This process will be decoupled in a future release. After a new version is released, the switch is complete and the production documentation reflects the specified current branch.

### Update the release process

When you publish from specific version branches, you need to bump the version branch as part of the release process.

Add an action as part of that repo’s release process for the release manager to update this same assembler file and bump the `current` branch with each release, as appropriate. The `next` branch also needs to be bumped if it is not set to `main`. 

When these releases happen, create a PR against the [assembler file](https://github.com/elastic/docs-builder/blob/main/src/tooling/docs-assembler/assembler.yml) that defines the new `current` branch, to merge on release day.


## Workflow 1 (default): Continuous deployment

Learn how to make updates in the continuous deployment branching strategy, where the repo is publishing docs from `main`.

### Where to make docs changes [make-changes-cd]

Initiate the changes by opening a PR against the `main` branch of the repo.

### How to write those changes [write-changes-cd]

In elastic.co/docs (Docs V3), we [write docs cumulatively](cumulative-docs.md) regardless of the branching strategy selected.

### Merging and backporting [merge-backport-cd]

When a repo publishes docs from its `main` branch, any merged changes are published within 30 minutes. It is then very important to consider the timing of the merge depending on the documented product:

| | Case | Approach |
| --- | --- | --- |
| 1 | You are documenting changes for an unversioned product (typically Serverless or Elastic Cloud), and the changes should only go live when the corresponding code or feature is available to users. | The PR should be merged on or after the release date of the feature. |
| 2 | You are documenting changes for a versioned product (any Stack components, ECE, ECK, etc.). | You have the choice between merging the PR as soon as it is approved, or merging it only on release day.<br><br>We have an automatic mechanism in place as part of the [cumulative docs strategy](cumulative-docs.md) that will show any changes published before its associated code or feature is available as `Planned`. |
| 3 | You are documenting changes that apply to both versioned and unversioned products (typically a change that is being released for both Serverless and an upcoming Stack release). | The PR should only be merged on or after the release date of the feature in Serverless.<br><br>For versioned products, we have an automatic mechanism in place as part of the [cumulative docs strategy](cumulative-docs.md) that will show any changes published before its associated code or feature is available as `Planned`. |

When a repo is publishing docs from its `main` branch, no backporting is needed.

:::{tip}
If you don’t want to hold on too many PRs to publish on release day, merge them to a feature branch, so you only have to merge this feature branch to `main` on release day.
:::

## Workflow 2: Tagged

Learn how to make updates in the continuous deployment branching strategy, where the repo is publishing docs from a specific `version` branch. 

### Where to make docs changes [make-changes-td]

Initiate the changes by opening a PR against the `main` branch of the repo. The changes will be backported to the relevant version branches as detailed below.

### How to write those changes [write-changes-td]

In elastic.co/docs (Docs V3), we [write docs cumulatively](cumulative-docs.md) regardless of the branching strategy selected.

### Merging and backporting [merge-backport-td]

When a repo publishes docs from a version branch, there is no timing constraint to merge the initial PR against the `main` branch. If `main` is set as your `next` branch, then the docs changes become visible on the internal staging docs site at [staging-website.elastic.co/docs](http://staging-website.elastic.co/docs). Otherwise, the docs changes become visible on the internal staging docs site when you backport to your `next` branch.

The changes must then be backported to their relevant version branches, and no further back than the branch set as `current` for the documentation publication system:

| | Case | Approach |
| --- | --- | --- |
| 1 | You are documenting changes for an unversioned product (typically Serverless or Elastic Cloud), the changes should go live when the corresponding code or feature is available to users. | The PR should be backported to the docs `current` branch, and any intermediate version branches that already exist between `current` and `main`. Merge the backport PR for `current` only when you’re sure the corresponding feature is released. |
| 2 | You are documenting changes for a versioned product (any Stack components, ECE, ECK, etc.). | Backport the PR to the relevant version branch and to any intermediate version branch that already exists. The changes will go live whenever these branches become the `current` docs branch.<br><br>We have an automatic mechanism in place as part of the [cumulative docs strategy](cumulative-docs.md) that will show any changes published before its associated code or feature is available as `Planned`. |
| 3 | You are documenting changes that apply to both versioned and unversioned products (typically a change that is being released for both Serverless and an upcoming Stack release). | The PR should be backported to the docs `current` branch, and any intermediate version branches that already exist between `current` and `main`. Merge the backport PR for `current` only when you’re sure the corresponding feature is released. <br><br>For versioned products, we have an automatic mechanism in place as part of the [cumulative docs strategy](cumulative-docs.md) that will show any changes published before its associated code or feature is available as `Planned`. |

#### Example [example-td]

For example, in a situation where 9.0, 9.1, and 9.2 are already released, and the 9.3 branch has already been cut:

* The branch set as `current` in the [docs assembler](https://github.com/elastic/docs-builder/blob/625e75b35841be938a8df76a62deeee811ba52d4/src/tooling/docs-assembler/assembler.yml#L70) is 9.2.  
* The branch set as `next` (where the content development first happens), is `main`.  
* 9.4 changes are only done on the `main` branch.  
* 9.3 changes are done on the `main` branch and backported to the 9.3 branch.  
* 9.1 changes are done on the `main` branch and backported to the 9.3 and 9.2 branches. Since 9.2 is the current docs branch, no need to go further.  
* Serverless changes are done on the `main` branch. They are backported to the `current` docs branch and any intermediate branches. In this case: 9.3 and 9.2.  
* Changes not specific to a version are done on the `main` branch and backported to all branches down to the `current` docs branch. In this case: 9.3 and 9.2.

:::{note}
While you *can* backport to versions prior to the `current` version when applicable to maintain parity between the code and the docs on a given branch, that content will not be used in the current state of the docs.
:::
