---
navigation_title: Contribute
---

# Elastic Docs contribution guide

Welcome, **contributor**!

Whether you're a technical writer or you've only edited Elastic docs once or twice, you're a valued contributor. Every word matters!

## Contribute to the docs [#contribute]

The version of the docs you want to contribute to determines the tool and syntax you must use to update the docs. The easiest way to find the source file for the page you want to update is to navigate to the page on the web and click **Edit this page**. This will take you to the source file in the correct repo. For more detailed instructions, see [Contribute on the web](on-the-web.md).

### Contribute to version `8.x` docs and earlier

To contribute to earlier versions of the Elastic Stack, you must work with our [legacy documentation build system](https://github.com/elastic/docs). This system uses the [AsciiDoc](https://asciidoc.org) markup language. 

* For **simple bugfixes and enhancements** --> [Contribute on the web](on-the-web.md)
* For **complex or multi-page updates** --> See the [legacy documentation build guide](https://github.com/elastic/docs?tab=readme-ov-file#building-documentation)

If you need to update documentation for both 8.x and 9.x, you will have to work in two systems. See [What if I need to update both 8.x and 9.x docs?](https://docs-v3-preview.elastic.dev/elastic/docs-builder/tree/main/contribute/on-the-web#what-if-i-need-to-update-both-8.x-and-9.x-docs) for the processes on updating docs across different versions.

### Contribute to version `9.0` docs and later

To contribute to `9.0+` content, you need to work with our new documentation system. This system uses custom [Markdown syntax](../syntax/index.md).
This content lives in one of three places:

1. **Reference/settings content** lives in the new Markdown files in the code repo as it did pre 9.0. This enables folks to make updates that touch code and low-level docs in the same PR.  
2. **Narrative/conceptual and overview content** lives in [elastic/docs-content](https://github.com/elastic/docs-content). For example, the [ES|QL overview](https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/explore-analyze/query-filter/languages/esql) lives in the **Explore & Analyze** section of the narrative docs. But the reference documentation lives in the Elasticsearch reference section of the docs: [ES|QL reference](https://docs-v3-preview.elastic.dev/elastic/elasticsearch/tree/main/reference/query-languages/esql)  
3. **API reference docs** live in the different OpenAPI spec repositories. This is where you need to update API docs published in the new API docs system.

When you are ready to edit the content:

* For **simple bugfixes and enhancements** --> [contribute on the web](on-the-web.md)
* For **complex or multi-page updates** --> [Contribute locally](locally.md)

## Where does the content live

Understand where your content of interest now sits in the new docs information architecture and the repos where source pages live.

### Kibana

#### What has moved to elastic/docs-content?

Public-facing narrative and conceptual docs have moved. Most can now be found under the following directories in the new docs:

* **explore-analyze**: Discover, Dashboards, Visualizations, Reporting, Alerting, dev tools...  
* **deploy-manage**: Stack management (Spaces, user management, remote clusters...)  
* **troubleshooting**: troubleshooting pages

#### What is staying in the Kibana repo?

* Reference content, anything that is or could be auto-generated: Settings, syntax references, functions lists...  
* Release notes  
* Developer guide

### Elasticsearch

#### What has moved to elastic/docs-content?

Public-facing narrative and conceptual docs have moved. Most can now be found under the following directories in the new docs:

* **explore-analyze**: query languages, scripting, machine learning (NLP)
* **deploy-manage**: deploying elastic, production guidance, security, and Management  
* **manage-data**: data store, migration, data lifecycle  
* **solutions**: core search, playground, hybrid search  
* **troubleshooting**: troubleshooting pages

#### What is staying in the Elasticsearch repo?

* Reference content (anything that is or could be auto-generated): Settings, syntax references, functions lists…  
* Contribution guides and extending elasticsearch (clients, etc)  
* Release notes

### Cloud

#### What has moved to elastic/docs-content?

Public-facing narrative and conceptual docs have moved. Most can now be found under the following directories in the new docs:

* **deploy-manage**: deploying cloud
* **cloud-account**: managing your cloud organization  
* **troubleshooting**: troubleshooting pages

#### **What is staying in the Cloud repo?**

* Reference content (= anything that is or could be auto-generated): Settings, syntax references, functions lists…  
* Release notes

### Machine learning

#### What has moved to elastic/docs-content?

Public-facing narrative and conceptual docs have moved. Most can now be found under the following directories in the new docs:

* **deploy-manage**: deploying cloud, managing your cloud organization  
* **troubleshooting**: troubleshooting pages

#### What is staying in the Elasticsearch repo?

* Reference content, anything that is or could be auto-generated): Settings, syntax references, functions lists…  
* Release notes

## How is content organized across documentation sections?

The following section shows a mapping of the different areas in the docs IA to the product areas and topics.

| **Area** | **Description** | **Sources (Area / Topic)** |
| ----- | ----- | ----- |
| **Get started** | Content in this section focuses on learning about Elasticsearch and the stack, learning about how it can be deployed, and basic environment setup for initial exploration. | Overview of Elastic and various topics |
| **Solutions and use cases** | Content in this section focuses on the core solutions themselves and their user cases. | **Search:** core search solution content, playground, hybrid search <br>**Observability:** core observability content <br>**Security:** core security content  |
| **Manage data** | Content in this section focuses on learning about Elastic data store primitives, evaluating and implementing ingestion and data enrichment technologies, managing the lifecycle of your data and migrating data between clusters. | **Elasticsearch:** data store, migration, data lifecycle <br>**Ingest:** ingesting data into Elasticsearch <br>**Kibana**: Discover, Dashboards, Visualizations, Reporting, Alerting, dev tools. |
| **Explore and analyze** | Content in this section focuses on querying, visualizing, and exploring data. Additionally it focusing on leveraging machine learning for data analysis and defining and responding to alerts |  **Elasticsearch:**  query languages, scripting, machine learning (NLP)  <br>**Machine Learning:** Anomaly detection, data frame analytics, NLP <br>**Kibana**: Discover, Dashboards, Visualizations, Reporting, Alerting, dev tools. <br>**ResponseOps:** Alerts and Cases |
| **Deploy and manage** | Content in this section focuses on evaluating deployment options and setting your environment up. This section also contains content around managing, securing, and monitoring your cluster.  | **Elasticsearch:** deploying elastic, production guidance, security, and Management <br>**Cloud:** deploying and managing Elastic Cloud, Elastic Cloud Enterprise, Serverless, and ECK. |
| **Manage your Cloud account** | Content in this section focuses specifically on managing Cloud accounts. | **Cloud:** Managing your cloud account |
| **Troubleshoot** | Content in this section is troubleshooting content for the different products as well as how to contact support. | All troubleshooting content |
| **Extend and contribute** | Content in this section focuses on contributing to Elastic products and building plugins, clients, and beats modules. | **Contributions guides for:** Kibana Logstash Beats <br>**Developer guides for:** Integrations Elasticsearch Plugins |
| **Reference** | Content in this section focuses on manuals for optional products as well as reference materials like query language references, configuration references, etc. | Reference content, Release notes, Developer guide |

## Report a bug

* It's a **documentation** problem --> [Open a docs issue](https://github.com/elastic/docs-content/issues/new?template=internal-request.yaml) *or* [Fix it myself](locally.md)
* It's a **build tool (docs-builder)** problem --> [Open a bug report](https://github.com/elastic/docs-builder/issues/new?template=bug-report.yaml)

## Request an enhancement

* Make the **documentation** better --> [Open a docs issue](https://github.com/elastic/docs-content/issues/new?template=internal-request.yaml)
* Make our **build tool (docs-builder)** better --> [Start a docs-builder discussion](https://github.com/elastic/docs-builder/discussions)

## Work on docs-builder

That sounds great! See [development](../development/index.md) to learn how to contribute to our documentation build system.

