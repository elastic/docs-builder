# index

Index a single documentation set to Elasticsearch, calls `docs-builder --exporters elasticsearch`. Exposes more options

## Usage

```
index [options...] [-h|--help] [--version]
```

## Options

`-es|--endpoint <string?>`
:   Elasticsearch endpoint, alternatively set env DOCUMENTATION_ELASTIC_URL (Default:   null)

`--path` `<string?>`
:   path to the documentation folder, defaults to pwd. (Default:   null)

`--api-key` `<string?>`
:   Elasticsearch API key, alternatively set env DOCUMENTATION_ELASTIC_APIKEY (Default:   null)

`--username` `<string?>`
:   Elasticsearch username (basic auth), alternatively set env DOCUMENTATION_ELASTIC_USERNAME (Default:   null)

`--password` `<string?>`
:   Elasticsearch password (basic auth), alternatively set env DOCUMENTATION_ELASTIC_PASSWORD (Default:   null)

`--no-semantic` `<bool?>`
:   Index without semantic fields (Default:   null)

`--search-num-threads` `<int?>`
:   The number of search threads the inference endpoint should use. Defaults:   8 (Default:   null)

`--index-num-threads` `<int?>`
:   The number of index threads the inference endpoint should use. Defaults:   8 (Default:   null)

`--bootstrap-timeout` `<int?>`
:   Timeout in minutes for the inference endpoint creation. Defaults:   4 (Default:   null)

`--index-name-prefix` `<string?>`
:   The prefix for the computed index/alias names. Defaults:   semantic-docs (Default:   null)

`--buffer-size` `<int?>`
:   The number of documents to send to ES as part of the bulk. Defaults:   100 (Default:   null)

`--max-retries` `<int?>`
:   The number of times failed bulk items should be retried. Defaults:   3 (Default:   null)

`--debug-mode` `<bool?>`
:   Buffer ES request/responses for better error messages and pass ?pretty to all requests (Default:   null)

`--proxy-address` `<string?>`
:   Route requests through a proxy server (Default:   null)

`--proxy-password` `<string?>`
:   Proxy server password (Default:   null)

`--proxy-username` `<string?>`
:   Proxy server username (Default:   null)

`--disable-ssl-verification` `<bool?>`
:   Disable SSL certificate validation (EXPERT OPTION) (Default:   null)

`--certificate-fingerprint` `<string?>`
:   Pass a self-signed certificate fingerprint to validate the SSL connection (Default:   null)

`--certificate-path` `<string?>`
:   Pass a self-signed certificate to validate the SSL connection (Default:   null)

`--certificate-not-root` `<bool?>`
:   If the certificate is not root but only part of the validation chain pass this (Default:   null)