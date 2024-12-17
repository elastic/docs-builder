---
title: "Semantic search with the inference API"
---

Semantic search helps you find data based on the intent and contextual meaning of a search query, instead of a match on query terms (lexical search).

In this tutorial, learn how to use the inference API workflow with various services to perform semantic search on your data.

```{admonition} Select your service

{bdg-link-primary-line}`Amazon Bedrock <amazon-bedrock.html>`
{bdg-link-primary-line}`Azure AI Studio <azure-ai-studio.html>`
{bdg-link-primary-line}`Azure OpenAI <azure-openai.html>`
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

Semantic search is a search method that helps you find data based on the intent and contextual meaning of a search query, instead of a match on query terms (lexical search).

## Model details

The examples in this tutorial use Cohere's `embed-english-v3.0` model, the `all-mpnet-base-v2` model from HuggingFace, and OpenAI's `text-embedding-ada-002` second generation embedding model.
You can use any Cohere and OpenAI models, they are all supported by the infrerence API.
For a list of recommended models available on HuggingFace, refer to the supported model list.

Azure based examples use models available through [Azure AI Studio](https://ai.azure.com/explore/models?selectedTask=embeddings)
or [Azure OpenAI](https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models).
Mistral examples use the `mistral-embed` model from the [Mistral API](https://docs.mistral.ai/getting-started/models/).
Amazon Bedrock examples use the `amazon.titan-embed-text-v1` model from the [Amazon Bedrock base models](https://docs.aws.amazon.com/bedrock/latest/userguide/model-ids.html).

```{tip}
Not seeing the tutorial? Select a service above to get started.
```
