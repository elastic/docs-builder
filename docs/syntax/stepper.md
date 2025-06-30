# Stepper

Steppers provide a visual representation of sequential steps, commonly used in tutorials or guides
to break down processes into manageable stages.

By default every step title is a link with a generated anchor.
But you can override the default anchor by adding the `:anchor:` option to the step.

## Basic Stepper

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

## Advanced Example

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

## Table of Contents Integration

Stepper step titles automatically appear in the page's "On this page" table of contents (ToC) sidebar, making it easier for users to navigate directly to specific steps.

### How it works

- **Automatic inclusion**: Step titles are automatically detected and added to the ToC during document parsing.
- **Proper nesting**: Steps appear as sub-items under their parent section heading with appropriate indentation.
- **Clickable navigation**: Users can click on step titles in the ToC to jump directly to that step.

### Nested steppers

When steppers are nested inside other directive components (like `{tab-set}`, `{dropdown}`, or other containers), their step titles are **not** included in the ToC to avoid:

- Duplicate or competing headings across multiple tabs
- Links to content that might be collapsed or hidden
- General confusion about content hierarchy

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

**Example of included stepper:**
```markdown
## Installation Guide

::::{stepper}
:::{step} Download
Download the software...
:::

:::{step} Install  
Run the installer...
:::
::::
```

In the second example, both "Download" and "Install" steps will appear in the ToC as sub-items under "Installation Guide".

## Dynamic heading levels

Stepper step titles automatically adjust their heading level based on the preceding heading in the document, ensuring proper document hierarchy and semantic structure.

### Examples

**Stepper after H2 heading:**
```markdown
## Installation Guide

::::{stepper}
:::{step} Download
Step titles render as H3 (visible in ToC)
:::
::::
```

**Stepper after H3 heading:**
```markdown
### Setup Process

::::{stepper}
:::{step} Configure
Step titles render as H4 (not in ToC, but proper hierarchy)
:::
::::
```

**Stepper with no preceding heading:**
```markdown
::::{stepper}
:::{step} First Step
Step titles default to H2 level
:::
::::
```
