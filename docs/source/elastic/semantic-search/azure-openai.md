---
title: "Semantic search with the inference API"
navigation_title: "Azure OpenAI"
---

Semantic search helps you find data based on the intent and contextual meaning of a search query, instead of a match on query terms (lexical search).

In this tutorial, learn how to use the inference API workflow with various services to perform semantic search on your data.


```{admonition} Select your service

{bdg-link-primary-line}`Amazon Bedrock <amazon-bedrock.html>`
{bdg-link-primary-line}`Azure AI Studio <azure-ai-studio.html>`
{bdg-link-primary}`Azure OpenAI <azure-openai.html>`
{bdg-link-primary-line}`Cohere <cohere.html>`
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

## Azure OpenAI

The examples in this tutorial use models available through [Azure OpenAI](https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models).

## Requirements

* An [Azure subscription](https://azure.microsoft.com/free/cognitive-services?azure-portal=true)
* Access granted to Azure OpenAI in the desired Azure subscription. You can apply for access to Azure OpenAI by completing the form at [https://aka.ms/oai/access](https://azure.microsoft.com/free/cognitive-services?azure-portal=true).
* An embedding model deployed in [Azure OpenAI Studio](https://azure.microsoft.com/free/cognitive-services?azure-portal=true).

## Create an inference endpoint

```{include} _snippets/inference-endpoint.md
```

```{code-block} bash
:linenos:
:caption: Create inference example for `Azure OpenAI`
:emphasize-lines: 5-9
PUT _inference/text_embedding/azure_openai_embeddings
{
    "service": "azureopenai",
    "service_settings": {
        "api_key": "<api_key>",
        "resource_name": "<resource_name>",
        "deployment_id": "<deployment_id>",
        "api_version": "2024-02-01"
    }
}
```

* The task type is `text_embedding` in the path and the `inference_id` which is the unique identifier of the inference endpoint is `amazon_bedrock_embeddings`.
* The access key can be found on your AWS IAM management page for the user account to access Amazon Bedrock.
* The secret key should be the paired key for the specified access key.
* Specify the region that your model is hosted in.
* Specify the model provider.
* The model ID or ARN of the model to use.

## Create the index mapping

```{include} _snippets/index-mapping.md
```

```{code-block} bash
:linenos:
:caption: Create index mapping for `Azure OpenAI`
:emphasize-lines: 6-12
PUT azure-openai-embeddings
{
  "mappings": {
    "properties": {
      "content_embedding": {
        "type": "dense_vector",
        "dims": 1536,
        "element_type": "float",
        "similarity": "dot_product"
      },
      "content": {
        "type": "text"
      }
    }
  }
}
```

* The name of the field to contain the generated tokens. It must be referenced in the inference pipeline configuration in the next step.
* The field to contain the tokens is a `dense_vector` field.
* The output dimensions of the model. Find this value in the [Azure OpenAI documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models#embeddings-models) of the model you use.
* For Azure OpenAI embeddings, the `dot_product` function should be used to calculate similarity as Azure OpenAI embeddings are normalised to unit length. See the [Azure OpenAI embeddings](https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/understand-embeddings)documentation for more information on the model specifications.
* The name of the field from which to create the dense vector representation. In this example, the name of the field is `content`. It must be referenced in the inference pipeline configuration in the next step.
* The field type which is text in this example.