# Attributes

To promote consistency across documentation, AsciiDoc uses shared attributes for common terms, URLs, and versions.

In the AsciiDoctor-based system, shared attributes are defined at the site-level and live in the [`shared/` directory](https://github.com/elastic/docs/blob/master/shared) in the elastic/docs repo. The most used files in this directory are:

* The [`attributes.asciidoc` file](https://github.com/elastic/docs/blob/master/shared/attributes.asciidoc), which contains URLs, common words and phrases, and more.
* The files in the [`versions/stack` directory](https://github.com/elastic/docs/tree/master/shared/versions/stack), which contain the latest versions for various products for a given Stack version.

## In `docs-builder`

Attributes are defined at the content set-level. Use the `subs` key to define attributes as key-value pairs in the content sets `docset.yml` or `toc.yml` file.

Example:

```yml
subs:
  attr-name:   "attr value"
  ea:   "Elastic Agent"
  es:   "Elasticsearch"
```

## Use attributes

Attributes can be referenced in documentation file with the following syntax: `{{attr-name-here}}`.
