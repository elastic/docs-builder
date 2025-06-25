# Contribute on the web

This section will help you understand how to update and contribute to our documentation post-migration.

## Updating documentation

:::{include} _snippets/two-systems.md
:::

### Update elastic.co/guide [guide]

:::{include} _snippets/guide-intro.md
:::

These changes should be made in the original source folders in their respective repositories. Here’s how you can do it:

1. Navigate to the page that is impacted.  
2. Click the **Edit** button.  
3. Ensure the targeted branch is \<insert proper branch\>.  
4. Make the necessary updates.  
5. Commit your changes and create a pull request.  
6. Add the appropriate labels per repo as found at [Page: Working across docs repos](https://elasticco.atlassian.net/wiki/spaces/DOC/pages/61604182/Working+across+docs+repos)

:::{note}
If you are working in a repo like Kibana or the cloud repo, backports can be complicated. You can use the [backport tool](https://github.com/sorenlouv/backport) to manage your backport.
:::

### Update elastic.co/docs [docs]

:::{include} _snippets/docs-intro.md
:::

For content hosted on elastic.co/docs, most conceptual and narrative content is stored in the [`docs-content`](https://github.com/elastic/docs-content) repository, and most reference content is hosted in the relevant product's repository. Follow these steps to ensure your contributions are correctly made:

1. Navigate to the page that is impacted.  
2. Click the **Edit** button.  
3. Identify the section that requires updates.  
4. Make the necessary updates.  
5. Commit your changes and create a pull request.

:::{include} tagged-warning.md
:::

## What if I need to update docs in both systems?

If you need to merge changes that are published in both systems (usually because a change is valid in multiple product versions, such as stack 9.x and 8.x) it is recommended to update the documentation in elastic.co/docs first. Then you can convert the updates to ASCIIDoc and make the changes to the elastic.co/guide documentation. To do this, follow these steps:

1. Install [pandoc](https://pandoc.org/installing.html) to convert your markdown file to asciidoc  
2. Update the /docs content first in markdown as described in [Update elastic.co/docs](#docs) in the relevant repository.  
3. Run your changes through pandoc:  
   1. If you need to bring over the entire file, you can run the following command and it will create an asciidoc file for you. `pandoc -f gfm -t asciidoc ./<file-name>.md -o <file-name>.asciidoc`  
   2. If you just need to port a specific section you can use: `pandoc -f gfm -t asciidoc ./<file-name>.md` and the output of the file will be in your command window from which you can copy.  
4. Follow the steps in [Update elastic.co/guide](#guide) to publish your changes.   
5. If the change is too large or complicated, create a new issue in the [`docs-content`](https://github.com/elastic/docs-content) or [`docs-content-internal`](https://github.com/elastic/docs-content-internal) repository detailing the changes made for the team to triage.  
6. Merge the changes and close the issue once the updates are reflected in the documentation.

## Migration considerations

During the migration, content may be moved around, and there won't be a 1-to-1 mapping between old and new locations. This means updates may need to be applied in multiple places. If your changes affect /guide content, consider merging those changes in the /docs content first and then add it to the appropriate /guide content. If you have any issues, create an issue in the [`docs-content`](https://github.com/elastic/docs-content) or [`docs-content-internal`](https://github.com/elastic/docs-content-internal) repository.