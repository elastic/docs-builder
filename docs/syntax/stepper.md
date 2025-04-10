# Stepper

## Numbered Stepper

::::::{tab-set}

:::::{tab-item} Output

::::{stepper}

1. ### Create an index [getting-started-index-creation]

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


2. ### Add data to your index

   :::{tip}
   This tutorial uses Elasticsearch APIs, but there are many other ways to [add data to Elasticsearch](#).
   :::

   You add data to Elasticsearch as JSON objects called documents. Elasticsearch stores these documents in searchable indices.

3. ### Define mappings and data types

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

:::::{tab-item} Markdown

````markdown
::::{stepper}

1. ### Create an index [getting-started-index-creation]

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


2. ### Add data to your index

   :::{tip}
   This tutorial uses Elasticsearch APIs, but there are many other ways to [add data to Elasticsearch](#).
   :::

   You add data to Elasticsearch as JSON objects called documents. Elasticsearch stores these documents in searchable indices.

3. ### Define mappings and data types

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
````

:::::

::::::
