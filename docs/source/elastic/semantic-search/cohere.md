---
title: "Semantic search with the inference API"
navigation_title: "Cohere"
---

Semantic search helps you find data based on the intent and contextual meaning of a search query, instead of a match on query terms (lexical search).

In this tutorial, learn how to use the inference API workflow with various services to perform semantic search on your data.

```{admonition} Select your service

{bdg-link-primary-line}`Amazon Bedrock <amazon-bedrock.html>`
{bdg-link-primary-line}`Azure AI Studio <azure-ai-studio.html>`
{bdg-link-primary-line}`Azure OpenAI <azure-openai.html>`
{bdg-link-primary}`Cohere <cohere.html>`
{bdg-link-primary-line}`ELSER <elser.html>`
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

## Cohere

The examples in this tutorial use Cohere's `embed-english-v3.0` model.
You can use any Cohere model as they are all supported by the inference API.

## Requirements

A [Cohere account](https://cohere.com/) is required to use the inference API with the Cohere service.

## Create an inference endpoint

```{include} _snippets/inference-endpoint.md
```

```{code-block} bash
:linenos:
:caption: Create inference example for `Cohere`
:emphasize-lines: 5-9
PUT _inference/text_embedding/cohere_embeddings
{
    "service": "cohere",
    "service_settings": {
        "api_key": "<api_key>",
        "model_id": "embed-english-v3.0",
        "embedding_type": "byte"
    }
}
```

* The task type is `text_embedding` in the path and the `inference_id` which is the unique identifier of the inference endpoint is `cohere_embeddings`.
* The API key of your Cohere account. You can find your API keys in your Cohere dashboard under the [API keys section](https://dashboard.cohere.com/api-keys). You need to provide your API key only once. The [Get inference API](https://www.elastic.co/guide/en/elasticsearch/reference/current/get-inference-api.html) does not return your API key.
* The name of the embedding model to use. You can find the list of Cohere embedding models [here](https://docs.cohere.com/reference/embed).

```{note}
When using this model the recommended similarity measure to use in the dense_vector field mapping is `dot_product`. In the case of Cohere models, the embeddings are normalized to unit length in which case the `dot_product` and the `cosine` measures are equivalent.
```

## Create the index mapping

```{include} _snippets/index-mapping.md
```

```{code-block} bash
:linenos:
:caption: Create index mapping for `Cohere`
:emphasize-lines: 6-12
PUT cohere-embeddings
{
  "mappings": {
    "properties": {
      "content_embedding": {
        "type": "dense_vector",
        "dims": 1024,
        "element_type": "byte"
      },
      "content": {
        "type": "text"
      }
    }
  }
}
```

* The name of the field to contain the generated tokens. It must be refrenced in the inference pipeline configuration in the next step.
* The field to contain the tokens is a `dense_vector` field.
* The output dimensions of the model. Find this value in the Cohere documentation of the model you use.
* The name of the field from which to create the dense vector representation. In this example, the name of the field is `content`. It must be referenced in the inference pipeline configuration in the next step.
* The field type which is text in this example.