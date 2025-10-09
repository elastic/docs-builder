---
navigation_title: "index"
---

# assembler index

Index documentation to Elasticsearch, calls `docs-builder assembler build --exporters elasticsearch`. Exposes more options

## Usage

```
docs-builder assembler index [options...] [-h|--help] [--version]
```

## Options

`-es|--endpoint <string>`
:   Elasticsearch endpoint, alternatively set env DOCUMENTATION_ELASTIC_URL (optional)

`--environment` `<string>`
:   The --environment used to clone ends up being part of the index name (optional)

`--api-key` `<string>`
:   Elasticsearch API key, alternatively set env DOCUMENTATION_ELASTIC_APIKEY (optional)

`--username` `<string>`
:   Elasticsearch username (basic auth), alternatively set env DOCUMENTATION_ELASTIC_USERNAME (optional)

`--password` `<string>`
:   Elasticsearch password (basic auth), alternatively set env DOCUMENTATION_ELASTIC_PASSWORD (optional)

`--no-semantic` `<bool?>`
:   Index without semantic fields (optional)

`--search-num-threads` `<int?>`
:   The number of search threads the inference endpoint should use. Defaults:   8 (optional)

`--index-num-threads` `<int?>`
:   The number of index threads the inference endpoint should use. Defaults:   8 (optional)

`--bootstrap-timeout` `<int?>`
:   Timeout in minutes for the inference endpoint creation. Defaults:   4 (optional)

`--index-name-prefix` `<string>`
:   The prefix for the computed index/alias names. Defaults:   semantic-docs (optional)

`--buffer-size` `<int?>`
:   The number of documents to send to ES as part of the bulk. Defaults:   100 (optional)

`--max-retries` `<int?>`
:   The number of times failed bulk items should be retried. Defaults:   3 (optional)

`--debug-mode` `<bool?>`
:   Buffer ES request/responses for better error messages and pass ?pretty to all requests (optional)

`--proxy-address` `<string>`
:   Route requests through a proxy server (optional)

`--proxy-password` `<string>`
:   Proxy server password (optional)

`--proxy-username` `<string>`
:   Proxy server username (optional)

`--disable-ssl-verification` `<bool?>`
:   Disable SSL certificate validation (EXPERT OPTION) (optional)

`--certificate-fingerprint` `<string>`
:   Pass a self-signed certificate fingerprint to validate the SSL connection (optional)

`--certificate-path` `<string>`
:   Pass a self-signed certificate to validate the SSL connection (optional)

`--certificate-not-root` `<bool?>`
:   If the certificate is not root but only part of the validation chain pass this (optional)