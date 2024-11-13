---
title: "Semantic search with the inference API"
navigation_title: "ELSER"
---

Semantic search helps you find data based on the intent and contextual meaning of a search query, instead of a match on query terms (lexical search).

In this tutorial, learn how to use the inference API workflow with various services to perform semantic search on your data.

```{admonition} Select your service

{bdg-link-primary-line}`Amazon Bedrock <amazon-bedrock.html>`
{bdg-link-primary-line}`Azure AI Studio <azure-ai-studio.html>`
{bdg-link-primary-line}`Azure OpenAI <azure-openai.html>`
{bdg-link-primary-line}`Cohere <cohere.html>`
{bdg-link-primary}`ELSER <elser.html>`
{bdg-link-primary-line}`HuggingFace <#>`
{bdg-link-primary-line}`Mistral <#>`
{bdg-link-primary-line}`OpenAI <#>`
{bdg-link-primary-line}`Service Alpha <#>`
{bdg-link-primary-line}`Service Bravo <#>`
{bdg-link-primary-line}`Service Charlie <#>`
{bdg-link-primary-line}`Service Delta <#>`
{bdg-link-primary-line}`Service Echo <#>`
{bdg-link-primary-line}`Service Foxtrot <#>`
```

----

## ELSER

## Requirements

ELSER is a model trained by Elastic. If you have an Elasticsearch deployment, there is no further requirement for using the inference API with the `elser` service.

## Create an inference endpoint

```{include} _snippets/inference-endpoint.md
```

```{code-block} bash
:linenos:
:caption: Create inference example for `ELSER`
:emphasize-lines: 3-6
PUT _inference/sparse_embedding/elser_embeddings
{
  "service": "elser",
  "service_settings": {
    "num_allocations": 1,
    "num_threads": 1
  }
}
```

* The task type is `sparse_embedding` in the path and the inference_id which is the unique identifier of the inference endpoint is `elser_embeddings`.

You don’t need to download and deploy the ELSER model upfront, the API request above will download the model if it’s not downloaded yet and then deploy it.

## Create the index mapping

```{include} _snippets/index-mapping.md
```

```{code-block} bash
:linenos:
:caption: Create index mapping for `ELSER`
:emphasize-lines: 6-12
PUT elser-embeddings
{
  "mappings": {
    "properties": {
      "content_embedding": {
        "type": "sparse_vector"
      },
      "content": {
        "type": "text"
      }
    }
  }
}
```

* The name of the field to contain the generated tokens. It must be referenced in the inference pipeline configuration in the next step.
* The field to contain the tokens is a sparse_vector field for ELSER.
* The name of the field from which to create the dense vector representation. In this example, the name of the field is content. It must be referenced in the inference pipeline configuration in the next step.
* The field type which is text in this example.
