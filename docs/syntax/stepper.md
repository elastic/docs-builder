# Stepper

Steppers provide a visual representation of sequential steps, commonly used in tutorials or guides to break down processes into manageable stages. For example, you can usee steppers instead of numbered
section headings when documenting a supertask or a complex procedure. An example is the [Observability Get Started](https://www.elastic.co/docs/solutions/observability/get-started).

By default every step title is a link with a generated anchor. You can override the default anchor by adding the `:anchor:` option to the step.

## Basic stepper

:::::::{tab-set}
::::::{tab-item} Output
:::::{stepper}

::::{step} Install
First install the dependencies.
```shell
npm install
```
::::

::::{step} Build
Then build the project.
```shell
npm run build
```
::::

::::{step} Test
Finally run the tests.
```shell
npm run test
```
::::

::::{step} Done
::::

:::::

:::::
::::::

::::::{tab-item} Markdown
````markdown
:::::{stepper}

::::{step} Install
First install the dependencies.
```shell
npm install
```
::::

::::{step} Build
Then build the project.
```shell
npm run build
```
::::

::::{step} Test
Finally run the tests.
```shell
npm run test
```
::::

::::{step} Done
::::

:::::
````
::::::

:::::::

## Advanced example

:::::::{tab-set}

::::::{tab-item} Output

:::::{stepper}

::::{step} Create an index

Create a new index named `books`:

```console
PUT /books
```

The following response indicates the index was created successfully.

:::{dropdown} Example response
```console-result
{
  "acknowledged": true,
  "shards_acknowledged": true,
  "index": "books"
}
```
:::

::::

::::{step} Add data to your index
:anchor: add-data

:::{tip}
This tutorial uses Elasticsearch APIs, but there are many other ways to [add data to Elasticsearch](#).
:::

You add data to Elasticsearch as JSON objects called documents. Elasticsearch stores these documents in searchable indices.
::::

::::{step} Define mappings and data types

   When using dynamic mapping, Elasticsearch automatically creates mappings for new fields by default.
   The documents we’ve added so far have used dynamic mapping, because we didn’t specify a mapping when creating the index.

   To see how dynamic mapping works, add a new document to the `books` index with a field that doesn’t appear in the existing documents.

   ```console
   POST /books/_doc
   {
     "name": "The Great Gatsby",
     "author": "F. Scott Fitzgerald",
     "release_date": "1925-04-10",
     "page_count": 180,
     "language": "EN" <1>
   }
   ```
   1. The new field.
::::

:::::

::::::

::::::{tab-item} Markdown

````markdown
:::::{stepper}

::::{step} Create an index

Create a new index named `books`:

```console
PUT /books
```

The following response indicates the index was created successfully.

:::{dropdown} Example response
```console-result
{
  "acknowledged": true,
  "shards_acknowledged": true,
  "index": "books"
}
```
:::

::::

::::{step} Add data to your index
:anchor: add-data

:::{tip}
This tutorial uses Elasticsearch APIs, but there are many other ways to [add data to Elasticsearch](#).
:::

You add data to Elasticsearch as JSON objects called documents. Elasticsearch stores these documents in searchable indices.
::::

::::{step} Define mappings and data types

When using dynamic mapping, Elasticsearch automatically creates mappings for new fields by default.
The documents we’ve added so far have used dynamic mapping, because we didn’t specify a mapping when creating the index.

To see how dynamic mapping works, add a new document to the `books` index with a field that doesn’t appear in the existing documents.

   ```console
   POST /books/_doc
   {
     "name": "The Great Gatsby",
     "author": "F. Scott Fitzgerald",
     "release_date": "1925-04-10",
     "page_count": 180,
     "language": "EN" <1>
   }
   ```
1. The new field.
   ::::

:::::
`````
::::::

:::::::

## Table of contents integration

Stepper step titles automatically appear in the page's "On this page" table of contents (ToC) sidebar, making it easier for users to navigate directly to specific steps.

### Nested steppers

When steppers are nested inside other directive components (like `{tab-set}`, `{dropdown}`, or other containers), their step titles are **not** included in the ToC to avoid duplicate or competing headings across multiple tabs or links to content that might be collapsed or hidden.

**Example of excluded stepper:**
```markdown
::::{tab-set}
:::{tab-item} Tab 1
::{stepper}
:{step} This step won't appear in ToC
Content here...
:
::
:::
::::
```
## Dynamic heading levels

Stepper step titles automatically adjust their heading level based on the preceding heading in the document, ensuring proper document hierarchy and semantic structure.

For example, a stepper that follows an `##` heading renders each step title as `###`.

### Headings inside steps

You can add sub-headings inside a step to organise longer content. Each heading must be **at least one level deeper** than the step's rendered level. If you write a heading at the same level as the step (or higher), the build automatically adjusts it to the correct level and emits a hint diagnostic pointing to the offending line.

The following example intentionally uses the wrong heading level to demonstrate the auto-correction. This stepper follows a `###` heading, so its steps render as `####`. The `####` sub-heading inside the step is at the same level as the step itself — it is auto-adjusted to `#####` and a hint is emitted:

```
HINT: Heading level h4 inside a step renders at the same or higher level as the step itself (h4).
      It has been adjusted to h5 — write it as '#####' to avoid this hint.
```

:::::::{tab-set}
::::::{tab-item} Output
:::::{stepper}

::::{step} Configure
#### Advanced options

These options override the defaults.
::::

:::::
::::::

::::::{tab-item} Markdown
````markdown
:::::{stepper}

::::{step} Configure
#### Advanced options

These options override the defaults.
::::

:::::
````
::::::
:::::::

To suppress the hint, write the heading at the correct level from the start:

```markdown
::::{step} Configure
##### Advanced options

These options override the defaults.
::::
```
