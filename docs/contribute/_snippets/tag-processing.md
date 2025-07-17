`applies_to` tags are rendered as badges in the documentation output. They reproduce the "key + lifecycle status + version" indicated in the content sources.

Specifically for versioned products, badges will display differently when the `applies_to` key specifies a product version that has not been released to our customers yet.

* `Planned` (if the lifecycle is preview, beta, or ga)
  
  Example: {applies_to}`stack: ga 99.99`
* `Deprecation planned` (if the lifecycle is deprecated)
  
  Example: {applies_to}`stack: deprecated 99.99`
* `Removal planned` (if the lifecycle is removed) 

  Example: {applies_to}`stack: removed 99.99`

This is computed at build time (there is a docs build every 30 minutes). The documentation team tracks and maintains released versions for these products centrally in [`versions.yml`](https://github.com/elastic/docs-builder/blob/main/config/versions.yml). 
When multiple lifecycle statuses and versions are specified in the sources, several badges are shown.

:::{note}
Visuals and wording in the output documentation are subject to changes and optimizations.
:::
