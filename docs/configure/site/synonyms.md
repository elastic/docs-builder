# `synonyms.yml`

The [`synonyms.yml`](https://github.com/elastic/docs-builder/blob/main/config/synonyms.yml) file provides a way to define synonyms for our Serverless observability project. 

Synonyms updates are sent during Elasticsearch-specific export procedures in the CI workflow.

```yml
synonyms:
  - [ ".net", "c#", "csharp", "dotnet", "net" ]
  - [ "esql", "es|ql" ]
  - [ "motlp", "managed otlp" ]
  ```
