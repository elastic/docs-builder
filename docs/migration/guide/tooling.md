# Run migration tooling

Use the [adoc-to-md](https://github.com/elastic/adoc-to-md) conversion tool to migrate content sets from Asciidoc syntax to docs-builder syntax. Instructions to use the tool are in the readme file.


## Post-migration tooling

After migrating content from asciidoc to md, there is cleanup work to do. @colleen has created a script to handle this process for us. The script:

* Moves files to their new home in the new IA
* Nests content at a pre-selected depth
    * shortens directory and file names
* Adds frontmatter mapping files to their asciidoc equivalent

### File/Dir mappings

* [`shortened-slugs.js`](https://github.com/elastic/docs-helpers/blob/post-migration-sort/post-migration-sort/input/field-mapping/shortened-slugs.js)
* [`word-replacement.js`](https://github.com/elastic/docs-helpers/blob/post-migration-sort/post-migration-sort/input/field-mapping/word-replacement.js)

## Post-migration manual work

* Being tracked in https://github.com/elastic/docs-builder/issues/261

## What's next?

After running the migration tool, you can move and manipulate files while viewing a live preview of the content with docs-builder.