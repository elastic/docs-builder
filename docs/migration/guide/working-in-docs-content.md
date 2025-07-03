# Working in docs-content

## What’s included in the initial migrated content set

To start, you will only have access to pages in the `get-started`, `solutions`, `manage-data`, `explore-analyze`, `deploy-manage`, and `troubleshoot` sections. The migrated `reference`, `release-notes`, and `extend` content will be available in the next few days. As a result, any links to pages in the `reference`, `release-notes`, and `extend` sections will be absolute links to the legacy AsciiDoc page. We will update links to these sections when the content is ready to be migrated to its forever home.

## How to navigate the migrated files in docs-content

How the migrated content for each page is organized in the new IA depends on how you filled out [Elastic docs IA](https://docs.google.com/spreadsheets/d/1LfPI3TZqdpONGxOmL8B8V-Feo1flLwObz9_ibCEMkIQ/edit?gid=502629814#gid=502629814).

**Frontmatter**
In Markdown files, there are two possible frontmatter options:

* **`navigation_title`**: This was added automatically if the AsciiDoc file that was migrated used `titleabbrev`.
* **`mapped_pages`**: This is a list of the AsciiDoc pages that are mapped to the new v3 page. **It is very important that you do not remove `mapped_pages` from the Markdown files** unless you’re moving the AsciiDoc page to a different v3 page’s list of `mapped_pages`. This is how we will keep track of the redirects we need to create when we end the documentation freeze.

**One-to-one mapping**
If there was just one AsciiDoc page mapped to the new v3 page, the migrated content was copied into a new Markdown file in its correct place in the new IA.

**Many-to-one mapping**
If there were multiple AsciiDoc pages mapped to a new v3 page:

* A stub Markdown file was added to its correct place in the new IA. This includes context from the IA inventory, including:
  * What needs to be done
  * Any scope notes
  * Linked GitHub issues
  * A list of migrated files to pull from when you’re building out the stub page
  * A list of any IDs (heading ID, paragraph ID, etc) that are necessary for internal links to work. As you start editing these files, you’ll need to incorporate these IDs or remove/update the links that point to these IDs.
* The migrated content from all the AsciiDoc pages mapped to the page are saved in the `raw-migrated-files` directory. That directory is organized by source repo and book (for example, a migrated file from the Security book will be in `raw-migrated-files/security-docs/security/`). These pages also appear in the rendered doc site at the bottom of the table of contents under *Content to pull from*.

**Zero-to-one mapping**
If there were no AsciiDoc pages mapped to the new v3 page, a stub file was created in its correct place in the new IA with any scope notes you added to the IA inventory. These pages won’t have `mapped_pages` in the frontmatter.

**Navigation files**
There are two types of navigation files:

* **`docset.yml`**: There is one doc set configuration file that compiles all the section `toc.yml` files.
* **`toc.yml`**: There is a table of contents file for each section.

**Images**
Images are in [one directory](https://github.com/elastic/docs-content/tree/main/images) at the top level of the docs-content repo. Please keep all images in this directory for now. We need to come up with a long-term strategy for images in this repository before making any changes.

## Dos and Don’ts

**It’s ok to:**

* Rename files and directories and move files between directories. Be sure to update links and image references when moving content.
  * We recommend using the `docs-builder mv` command to move files or folders. This command will move a single file or folder and automatically update any links for you. Use the command with:

			`docs-builder mv ./old-location/ia.md ./new-location/ia.md`

* More details on the `mv` command are in [https://github.com/elastic/docs-builder/pull/376](https://github.com/elastic/docs-builder/pull/376).
* Change the `h1` title.
* Change the `navigation_title` in the frontmatter.
* Add the [product availability](/syntax/applies.md) to frontmatter of each page.
* Make edits to the migrated content.
* Copy content from `raw-migrated-files` into the stub page files. Be sure to update links and image references when moving content.
  * Check if any other pages rely on the content in the `raw-migrated-files` file. You can either:
    * Refer to the [IA inventory](https://docs.google.com/spreadsheets/d/1LfPI3TZqdpONGxOmL8B8V-Feo1flLwObz9_ibCEMkIQ/edit?gid=605983744#gid=605983744).
    * Search across the docs-content repo for the name of the file to see if it shows up in other stub pages.
  * If no other pages rely on this file, delete the file from the `raw-migrated-files` directory.
  * If other pages do rely on this file, add a code comment to the file in the `raw-migrated-files` directory letting other writers know what content you used in the file.
* Report v3 bugs in [elastic/docs-builder](https://github.com/elastic/docs-builder/issues).

**Do not**:

* Remove the `mapped_pages` frontmatter.
* Nest Markdown files more than 3 levels deep within the docs-content repo. This has implications for URLs and SEO.
* Delete any IDs listed on stub pages without updating links that point to them throughout the docs.

## How to open a pull request

You must propose changes on a branch pushed to the upstream (elastic/docs-content) repository, not a personal clone.

When you open a PR to update files in docs-content, the `docs-preview/deploy` action will run. This action will build your PR and ensure your changes don’t introduce errors (like broken links). If the build succeeds, click *View deployment* to see a preview of your changes.

If the build fails, open the GitHub action to view the errors. You will not be able to merge your PR until all errors are resolved.

## Open `docs-builder` bugs and enhancements

As you start diving into the new system, you’ll likely encounter bugs or things that are missing that you’d like to see.

### Bugs

For bugs, open an issue [here](https://github.com/elastic/docs-builder/issues/new?template=bug-report.yaml).

### Feature requests

For feature requests, we’re now using GitHub Discussions. You can open a feature request [here](https://github.com/elastic/docs-builder/discussions/new?category=feature-request). We’re making the switch to Discussions because they offer a better way to brainstorm and gather feedback in a collaborative, async-friendly way (like threaded conversations for easier back-and-forth).

Please check out the growing list of feature requests from your fellow writers [here](https://github.com/elastic/docs-builder/discussions/new?category=feature-request). Vote on enhancements, share your thoughts, and add any ideas you have. Your feedback is crucial to ensure we’re prioritizing the right features and building the best system possible.
