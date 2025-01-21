# Code blocks

Code blocks can be used to display multiple lines of code. They preserve formatting and provide syntax highlighting when possible.

### Syntax

````markdown
```yaml
project:
  title: MyST Markdown
  github: https://github.com/jupyter-book/mystmd
```
````

```yaml
project:
  title: MyST Markdown
  github: https://github.com/jupyter-book/mystmd
```

### Asciidoc syntax

```markdown
[source,sh]
--------------------------------------------------
GET _tasks
GET _tasks?nodes=nodeId1,nodeId2
GET _tasks?nodes=nodeId1,nodeId2&actions=cluster:*
--------------------------------------------------
```

### Code blocks with callouts

A code block can include `<\d+>` at the end to indicate code callouts.
A code block with this style of callouts **needs** to be followed by an ordered list with an equal amount of items as called out.
Otherwise, the docs-builder will throw an error.

This syntax mimics what was implemented for our asciidoc system

````markdown
```yaml
project:
  title: MyST Markdown
  github: https://github.com/jupyter-book/mystmd
  license:
    code: MIT
    content: CC-BY-4.0 <1>
  subject: MyST Markdown
```

1. The license
````


### Magic Callout

You can include the callouts also directly as code using either `//` or `#` comments.

These will then be listed and numbered automatically

````markdown
```csharp
var apiKey = new ApiKey("<API_KEY>"); // Set up the api key
var client = new ElasticsearchClient("<CLOUD_ID>", apiKey);
```
````

Will output:

```csharp
var apiKey = new ApiKey("<API_KEY>"); // Set up the api key
var client = new ElasticsearchClient("<CLOUD_ID>", apiKey);
```

:::{note}
the comments have the follow code to be hoisted as a callout.
:::

````markdown
```csharp
// THIS IS NOT A CALLOUT
var apiKey = new ApiKey("<API_KEY>"); // However this is
var client = new ElasticsearchClient("<CLOUD_ID>", apiKey);
```
````

will output:

```csharp
// THIS IS NOT A CALLOUT
var apiKey = new ApiKey("<API_KEY>"); // However this is
var client = new ElasticsearchClient("<CLOUD_ID>", apiKey);
```
