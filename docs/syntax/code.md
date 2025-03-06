# Code

Code blocks can be used to display multiple lines of code. They preserve formatting and provide syntax highlighting when possible.

## Code block

Start and end a code block with a code fence. A code fence is a sequence of at least three consecutive backtick characters `` ``` ``. You can optionally add a language identifier to enable syntax highlighting.


::::{tab-set}

:::{tab-item} Output

```yaml
project:
  title: MyST Markdown
  github: https://github.com/jupyter-book/mystmd
```

:::

:::{tab-item} Markdown

````markdown
```yaml
project:
  title: MyST Markdown
  github: https://github.com/jupyter-book/mystmd
```
````

:::

::::

### Code callouts

There are two ways to add callouts to a code block. When using callouts, you must use one callout format. You cannot combine explicit and magic callouts.

#### Explicit callouts

Add one or more callouts to a code block by adding a comment (`#`  or `//` ) with a callout label at the end of the line. The callout label must be a number enclosed in angle brackets `<>`. 

An ordered list with the same number of items as callouts must follow the code block. If the number of list items doesn’t match the callouts, docs-builder will throw an error.

::::{tab-set}

:::{tab-item} Output

```yaml
project:
  license:
    content: CC-BY-4.0 # <1>
```

1. The license

:::


:::{tab-item} Markdown

````markdown
```yaml
project:
  license:
    content: CC-BY-4.0 # <1>
```

1. The license
````

:::

::::


#### Magic Callouts

If a code block contains code comments in the form of `//` or `#`, callouts will be magically created 🪄.


::::{tab-set}

:::{tab-item} Output

```csharp
var apiKey = new ApiKey("<API_KEY>"); // Set up the api key
var client = new ElasticsearchClient("<CLOUD_ID>", apiKey);
```

:::

:::{tab-item} Markdown

````markdown
```csharp
var apiKey = new ApiKey("<API_KEY>"); // Set up the api key
var client = new ElasticsearchClient("<CLOUD_ID>", apiKey);
```
````
:::


::::

Code comments must follow code to be hoisted as a callout. For example:

::::{tab-set}

:::{tab-item} Output

```csharp
// THIS IS NOT A CALLOUT
var apiKey = new ApiKey("<API_KEY>"); // This is a callout
var client = new ElasticsearchClient("<CLOUD_ID>", apiKey);
```

:::

:::{tab-item} Markdown

````markdown
```csharp
// THIS IS NOT A CALLOUT
var apiKey = new ApiKey("<API_KEY>"); // This is a callout
var client = new ElasticsearchClient("<CLOUD_ID>", apiKey);
```
````

:::

::::

### Console code blocks

:::{note}
This feature is still being developed.
:::

We document a lot of API endpoints at Elastic. For these endpoints, we support `console` as a language. The term console relates to the dev console in kibana which users can link to directly from these code snippets.

In a console code block, the first line is highlighted as a dev console string and the remainder as json:

::::{tab-set}

:::{tab-item} Output

```console
GET /mydocuments/_search
{
    "from": 1,
    "query": {
        "match_all" {}
    }
}
```

:::

:::{tab-item} Markdown

````markdown
```console
GET /mydocuments/_search
{
    "from": 1,
    "query": {
        "match_all" {}
    }
}
```
````

:::

::::

## Inline code

Use backticks to create an inline code span.
Inline code spans are useful for short code snippets or variable names.


### Inline code in a paragraph

::::{tab-set}

:::{tab-item} Output

This `code` is inline.

:::

:::{tab-item} Markdown

````markdown
This `code` is inline.
````
:::

::::

### Inline code in a heading

::::{tab-set}

:::{tab-item} Output

## This `code` is in a heading.

:::

:::{tab-item} Markdown

````markdown
## This `code` is in a heading.
````
:::

::::
