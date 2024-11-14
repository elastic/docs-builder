---
title: Images
---

## Top-level image

Here is a block-level image from the top-level static folder. The image directive supports attributes such as alt text and image width as shown here.

```{image} /_static/img/observability.png
:alt: Elastic Search
:width: 250px
```

Here is the same image used inline ![Elasticsearch](/_static/img/observability.png){width=30px}. Myst's `attr-inline` extension allows adding attributes to inline directives.
Here we used `w=30px` to make the image fit.

This is also inline:
![Elasticsearch](/_static/img/observability.png){width=200px align=center}
But we used `{w=200px align=center}`

## Local image

Here is an image from the local image folder. 

```{image} img/serverless-capabilities.svg
:alt: Elastic Search
:height: 400px
```

## Image with caption

We can use the `figure-md` directive to add caption to an image.

```{figure-md} /_static/img/observability.png
:width: 350px
:align: center

This is a caption in **Markdown**
```
