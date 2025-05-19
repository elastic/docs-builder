# Images

Images include screenshots, inline images, icons, and more. Syntax for images is like the syntax for links, with two differences:
1. instead of link text, you provide an image description
2. an image description starts with `![` not just `[`

Images can be referenced from the top-level `_static` dir or a local image dir.

## Block-level images

```markdown
![APM](images/apm.png)
```

![APM](images/apm.png)

Or, use the `image` directive.

```markdown
:::{image} images/observability.png
:alt: Elasticsearch
:width: 250px
:::
```

:::{image} images/observability.png
:alt: Elasticsearch
:width: 250px
:::

## Screenshots

Screenshots are images displayed with a box-shadow. Define a screenshot by adding the `:screenshot:` attribute to a block-level image directive.

```markdown
:::{image} images/apm.png
:screenshot:
:::
```

:::{image} images/apm.png
:screenshot:
:::

## Inline images

```markdown
Here is the same image used inline ![Elasticsearch](images/observability.png "elasticsearch =50%x50%")
```

Here is the same image used inline ![Elasticsearch](images/observability.png "elasticsearch =50%x50%")


### Inline image titles

Titles are optional making this the minimal syntax required

```markdown
![Elasticsearch](images/observability.png)
```

Including a title can be done by supplying it as an optional argument.

```markdown
![Elasticsearch](images/observability.png "elasticsearch")
```

### Inline image sizing

Inline images are supplied at the end through the title argument.

This is done to maintain maximum compatibility with markdown parsers
and previewers. 

```markdown
![alt](img.png "title =WxH")
![alt](img.png "title =W")
```

`W` and `H` can be either an absolute number in pixels or a number followed by `%` to indicate relative sizing.

If `H` is omitted `W` is used as the height as well.

```markdown
![alt](img.png "title =250x330")
![alt](img.png "title =50%x40%")
![alt](img.png "title =50%")
```



### SVG 

```markdown
![Elasticsearch](images/alerts.svg)
```
![Elasticsearch](images/alerts.svg)

### GIF

```markdown
![Elasticsearch](images/timeslider.gif)
```
![Elasticsearch](images/timeslider.gif)


## Asciidoc syntax

```asciidoc
[role="screenshot"]
image::images/metrics-alert-filters-and-group.png[Metric threshold filter and group fields]
```

```asciidoc
image::images/synthetics-get-started-projects.png[]
```
