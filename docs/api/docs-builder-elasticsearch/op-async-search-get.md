# Async search get

## Description

Retrieve the results of a previously submitted asynchronous search request.
If Elasticsearch security features are enabled, Elasticsearch restricts access to the user or API key that submitted the request.

This file is the local fixture for docs-builder development. The file name matches the `async-search-get` operation.

## Parameters

: `keep_alive`
  How long Elasticsearch keeps this search and its saved results.
  If you omit it, Elasticsearch uses the value from the matching submit request.
  If you extend it, Elasticsearch also extends the validity of the saved results.
  If the period expires while the search still runs, Elasticsearch cancels the search.
  If the search is complete, Elasticsearch deletes the saved results.

: id
  The async search id returned by the submit request.

## When to poll

Retry `GET /_async_search/{id}` until `is_running` is `false`.
If you need to wait on the server, set `wait_for_completion_timeout`. Do not poll from the client in that case.
