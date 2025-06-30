# Stepper

Steppers provide a visual representation of sequential steps, commonly used in tutorials or guides
to break down processes into manageable stages.

By default every step title is a link with a generated anchor.
But you can override the default anchor by adding the `:anchor:` option to the step.

## Basic Stepper

:::::::{tab-set}
::::::{tab-item} Output
:::::{stepper}

:::::{stepper}

::::{step}
### Install
First install the dependencies.
```shell
npm install
```
::::

::::{step}Build
### Build
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
