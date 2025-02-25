---
navigation_title: Move reference docs
---

# Move reference docs from Asciidocalypse

This page is divided into three sections:

1. [How Reference Content Works in V3](#how-reference-content-works-in-v3) – Useful for contributors who want to understand the difference between PR previews and full website builds.  
2. [How to Move Reference Content](#how-to-move-reference-content) – No longer relevant, as the process has been automated.  
3. [How to Manage Moved Reference Content](#how-to-manage-moved-reference-content) – Important for writers responsible for merging reference content.  

Jump to the relevant section based on your needs.

## How reference content works in V3

There are two use cases for building reference content in V3:
- **Preview builds**
- **elastic.co/docs builds**

Some repositories, like [`elastic/elastic-serverless-forwarder`](https://github.com/elastic/elastic-serverless-forwarder), contain a single chunk of content within the larger `elastic.co/docs` build. This means the structure of the content is the same for both preview builds and the final Elastic.co/docs build.

However, most repositories contain content that will live in multiple locations within the new `elastic.co/docs` IA. Consider `apm-agent-android` as an example. It has the following content:
- Reference docs
- Release notes
- Known issues
- Breaking changes
- Deprecations

### Directory Structure

Release notes do **not** require individual `toc.yml` files for each content set. The directory structure for `apm-agent-android` therefore follows this format:

```md
docs/
    `docset.yml`
        * Defines substitutions
        * Includes all `toc.yml` files in this repo
        * Ignored by the assembler
    reference/
        `toc.yml` (for this directory)
        All reference `.md` files go here
    release-notes/
        `toc.yml` (for this directory)
        All release notes, known issues, breaking changes, and deprecations files go here
    images/
        All image files for all content sets
```

### Preview Site vs. Full Site Structure

For individual repository preview builds, **all** content in the `docs/` directory builds together, appearing as:

```md
> Reference
   Page one
   Page two
   Page three

> Release notes
   > Known issues
          APM Android Agent
   > Breaking changes
          APM Android Agent
   > Deprecations
          APM Android Agent
   APM Android Agent release notes
```

This structure is defined in the repo's `docset.yml` file.

For the **full Elastic.co/docs site**, the assembler references the individual content set definitions (`toc.yml`) within the repo and organizes the content accordingly:

![Diagram of how everything maps together](../../images/great-drawing-of-new-structure.png)

## How to Move Reference Content

:::{note}
The moving of reference content has been automated. These docs will live on in the short term as a point of reference.
:::

The steps below explain how to move reference content. You can also take a look at our [sample PR](https://github.com/elastic/apm-agent-android/pull/398), which has specific commits to illustrate some of the steps below.

### Step 1: Delete Existing AsciiDoc Files

:::{important}
Skip this step for **any Cloud repos** and the **search-ui repository**.
:::

Ensure you only delete **external documentation files**.  
- In some repositories, this means deleting the entire `/docs` directory.  
- In others, manually verify which files should be removed.  
- Use [`conf.yaml`](https://github.com/elastic/docs/blob/master/conf.yaml) to determine what should and shouldn't be removed.

Example commit: [#398/commit](https://github.com/elastic/apm-agent-android/pull/398/commits/749803ae9bccdb9f8abdf27a5c7434350716b6c0)

### Step 2: Copy and Paste New Content

Move content from `asciidocalypse` to the correct directory in the target repo.  
Use [issue#130](https://github.com/elastic/docs-eng-team/issues/130) to determine the correct path structure.

Example commit: [#398/commit](https://github.com/elastic/apm-agent-android/pull/398/commits/3f966b0e1fa2f008da23d02f2c9e91a60c1bdf8d)

### Step 3: Add the new CI checks

There are two CI checks to add:

**`docs-build.yml`**
Add a file named `docs-build.yml` at `.github/workflows/docs-build.yml`. The contents of this file are below:

```yml
name: docs-build

on:
  push:
    branches:
      - main
  pull_request_target: ~
  merge_group: ~

jobs:
  docs-preview:
    uses: elastic/docs-builder/.github/workflows/preview-build.yml@main
    with:
      path-pattern: docs/**
    permissions:
      deployments: write
      id-token: write
      contents: read
      pull-requests: read
```

Learn more about this file: [`docs-build.yml`](./how-to-set-up-docs-previews.md#build).

:::{important}
If the documentation you are adding will not live in the `/docs/*` dir of the repository, you must update the `path-pattern` appropriately. Please reach out in #docs-team if you need help with this.
:::

**`docs-cleanup.yml`**
Add a file named `docs-cleanup.yml` at `.github/workflows/docs-cleanup.yml`. The contents of this file are below:

```yml
name: docs-cleanup

on:
  pull_request_target:
    types:
      - closed

jobs:
  docs-preview:
    uses: elastic/docs-builder/.github/workflows/preview-cleanup.yml@main
    permissions:
      contents: none
      id-token: write
      deployments: write
```

Learn more about this file: [`docs-cleanup.yml`](./how-to-set-up-docs-previews.md#cleanup)

Example PR: [#398](https://github.com/elastic/apm-agent-android/pull/398)

### Step 4: Delete the asciidoc warning

:::{important}
Skip this step for **any Cloud repos** and the **search-ui repository**.
:::

During the migration freeze, we added a check to each repository that warned when a PR was opened against asciidoc files in `main`. It is now safe to remove this file.

File to delete: `.github/workflows/comment-on-asciidoc-changes.yml`

Example commit: [#398/commit](https://github.com/elastic/apm-agent-android/pull/398/commits/be422934e79c5ecadd7b76523d2e1676fc86f323)

### Step 4: Wait for CI to Pass

Verify that all automated checks pass before proceeding. If you encounter any linking failures and need help resolving them, reach out in the typical docs channels.

### Step 5: Merge the PR

Once everything is confirmed working, merge the pull request.

### Step 6: Update the Tracking Issue

Update [issue#130](https://github.com/elastic/docs-eng-team/issues/130) to reflect the completed migration.

## Managing Moved Reference Content

You've been assigned to a repository in [issue #130](https://github.com/elastic/docs-eng-team/issues/130). Now what?

The good news: all necessary PRs have already been opened for you. Each repository has two PRs:

1. A PR that adds GitHub Actions for build previews ([example](https://github.com/elastic/ecs-logging/pull/85)).
2. A PR that removes AsciiDoc content and adds Markdown content ([example](https://github.com/elastic/ecs-logging/pull/84)).

### Your Role

Ideally, your job is to work with codeowners and repo admins to:

1. **Get the first PR merged** (to ensure previews work).
2. **Merge `main` into the second PR and get it merged**.

Splitting this into two PRs ensures that content is merged with a clean CI pass on the first attempt.

### Alternative Approach

Some repositories may be more challenging to work with. If needed, you can cherry-pick the commits into a single PR and collaborate with codeowners to get it merged. 

**Downside:** We won't know if CI passes until after merging.  
**Use your judgment**—choose the best approach for your situation.

### Key Considerations

Before merging, review the following:

- **Is the right content being deleted?** Ensure no essential internal docs are being removed.
- **Is the correct content being moved?** Double-check that everything is in its proper place.
- **Are tests passing?** If not, what adjustments are needed to make the content mergeable?

Let us know if you encounter any blockers. Thanks for your help!
