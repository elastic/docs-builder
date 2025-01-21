# File structure

In both the AsciiDoc- and V3-based systems, file structure is largely independent from the resulting site navigation.

## File and directory structure

AsciiDoc files and assets (like images and videos) can be stored anywhere within the directories provided as `sources` for a given book in the central [`config.yaml` file in the /docs repo](https://github.com/elastic/docs/blob/master/conf.yaml).

## Migration tool output

The migration tool will add all MD files in a single directory. There will be one MD file for each web page in the migrated book. The name of each MD file and the URL in the new docs system are both derived from the URL of the AsciiDoc-built page.

**Example:** Here's what happens with the
[Spans](https://www.elastic.co/guide/en/apm/guide/current/data-model-spans.html) page
in the APM docs:

* **Old URL**: The URL for the page in the old system is:
  `https://www.elastic.co/guide/en/apm/guide/current/data-model-spans.html`.
  From the URL, we can determine the:
    * _base URL_: `https://www.elastic.co/guide`
    * _book ID_: `en/apm/guide`
    * _version_: `current`
    * _page ID_: `data-model-spans`
* **New filename**: The page ID determines the filename of the migrated MD file:
% `data-model-spans.mdx`. This file will be in the root directory of the directory containing the content for the `en/apm/guide` book.
* **New URL**: The new URL for this page in the new docs system will be `xxxx`.

Because a single AsciiDoc file can contain the content for multiple pages (or content
displayed on a single page could be spread across multiple AsciiDoc files), the `.asciidoc`
filename can be different than the `.md` filename. However, you should be able to locate
the source content if you know which web page it lives on.

In the output from the migration tool the slug (for example, `en/apm/guide/data-model-spans`)
and the MD filename (for example, `data-model-spans.md`) are both derived from
the page ID, they don't _have_ to be the same.

### Assets

The migration tool creates an `images/` directory in the base directory for the doc set.
Inside the `images/` directory, there is subdirectory for each page ID that contains images.

For example, the images that are used on the [Entity Analytics dashboard](https://www.elastic.co/guide/en/security/current/detection-entity-dashboard.html) page in the AsciiDoc Security book would be copied to
the following location in the migrated docs:

```
images
  └─ detection-entity-dashboard
      ├─ dashboards-anomalies-table.png
      ├─ dashboards-entity-dashboard.png
      ├─ dashboards-host-score-data.png
      ├─ dashboards-run-job.png
      └─ dashboards-user-score-data.png
```

These can be reorganized post-migration.

### Reusable content

Reusable content is lost during migration.