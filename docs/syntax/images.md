# Images

Images include screenshots, inline images, icons, and more. Syntax for images is like the syntax for links, with the following differences:

1. instead of link text, you provide an image description
2. an image description starts with `![` not just `[`
3. there are restrictions on the scope of image paths

:::{note}

If a page uses an image that exists outside the folder that contains the `toc.yml` file or `docset.yml` file that contains that page, the image will fail to render and will generate warnings. Likewise, if a snippet in a [file inclusion](/syntax/file_inclusion.md) includes an image and is used in pages that exist in different `toc.yml`, the images will break.
:::

## Block-level images

```markdown
![APM](/syntax/img/apm.png)
```

![APM](/syntax/img/apm.png)

Or, use the `image` directive.

```markdown
:::{image} /syntax/img/observability.png
:alt: Elasticsearch
:width: 250px
:::
```

:::{image} /syntax/img/observability.png
:alt: Elasticsearch
:width: 250px
:::

## Screenshots

Screenshots are images displayed with a box-shadow. Define a screenshot by adding the `:screenshot:` attribute to a block-level image directive.

```markdown
:::{image} /syntax/img/apm.png
:screenshot:
:::
```

:::{image} /syntax/img/apm.png
:screenshot:
:::

## Inline images

```markdown
Here is the same image used inline ![Elasticsearch](/syntax/img/observability.png "elasticsearch =50%x50%")
```

Here is the same image used inline ![Elasticsearch](/syntax/img/observability.png "elasticsearch =50%x50%")


### Inline image titles

Titles are optional making this the minimal syntax required

```markdown
![Elasticsearch](/syntax/img/observability.png)
```

Including a title can be done by supplying it as an optional argument.

```markdown
![Elasticsearch](/syntax/img/observability.png "elasticsearch")
```

### Inline image sizing

Inline images are supplied at the end through the title argument.

This is done to maintain maximum compatibility with markdown parsers
and previewers. 

```markdown
![alt](/syntax/img/apm.png "title =WxH")
![alt](/syntax/img/apm.png "title =W")
```

`W` and `H` can be either an absolute number in pixels or a number followed by `%` to indicate relative sizing.

If `H` is omitted `W` is used as the height as well.

```markdown
![alt](/syntax/img/apm.png "title =250x330")
![alt](/syntax/img/apm.png "title =50%x40%")
![alt](/syntax/img/apm.png "title =50%")
```



### SVG 

```markdown
![Elasticsearch](/syntax/img/alerts.svg)
```
![Elasticsearch](/syntax/img/alerts.svg)

### GIF

```markdown
![Elasticsearch](/syntax/img/timeslider.gif)
```
![Elasticsearch](/syntax/img/timeslider.gif)


## Asciidoc syntax

```asciidoc
[role="screenshot"]
image::images/metrics-alert-filters-and-group.png[Metric threshold filter and group fields]
```

```asciidoc
image::images/synthetics-get-started-projects.png[]
```
