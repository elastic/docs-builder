
## **How do I know where to contribute?**

The easiest way to find the source file for the page you want to update is to navigate to the page on the web and click **Edit this page**. This will take you to the source file in the correct repo.

The following outlines the general guidelines though to help you determine where the content might be living.

### **9.0+ content**

For all 9.0+ content, it lives in one of three spots:

1. **Reference/settings content** lives in the new Markdown files in the code repo as it did pre 9.0. This enables folks to make updates that touch code \+ low-level docs in the same PR.  
2. **Narrative/conceptual and overview content** lives in [elastic/docs-content](https://github.com/elastic/docs-content). As an example the [ES|QL overview](https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/explore-analyze/query-filter/languages/esql) lives in the **Explore & Analyze** section of the narrative docs. But the reference documentation lives in the reference section of the docs [ES|QL reference](https://docs-v3-preview.elastic.dev/elastic/elasticsearch/tree/main/reference/query-languages/esql)  
3. **API reference docs** live in the different OpenAPI spec repositories. This is where you need to update API docs published in the new API docs system

### **All content prior to 9.0+**

If you are working in 8.x or below, you will use the existing asciidoc system. This means that if you need to update documentation for both 8.x and 9.x, you will have to work in two systems. See [What if I need to update both 8.x and 9.x docs?](https://docs-v3-preview.elastic.dev/elastic/docs-builder/tree/main/contribute/on-the-web#what-if-i-need-to-update-both-8.x-and-9.x-docs) for the processes on updating docs across different versions.

## Product areas

### **Kibana**

#### **What has moved to elastic/docs-content?**

Public-facing narrative and conceptual docs have moved. Most can now be found under the following directories in the new docs:

* explore-analyze: Discover, Dashboards, Visualizations, Reporting, Alerting, dev tools...  
* deploy-manage: Stack management (Spaces, user management, remote clusters...)  
* troubleshooting: troubleshooting pages

#### **What is staying in the Kibana repo?**

* Reference content (= anything that is or could be auto-generated): Settings, syntax references, functions lists...  
* Release notes  
* Developer guide

### **Elasticsearch**

#### **What has moved to elastic/docs-content?**

Public-facing narrative and conceptual docs have moved. Most can now be found under the following directories in the new docs:

* explore-analyze: query languages, scripting, machine learning (NLP)
* deploy-manage: deploying elastic, production guidance, security, and Management  
* Manage-data: data store, migration, data lifecycle  
* solutions: core search, playground, hybrid search  
* troubleshooting: troubleshooting pages

#### **What is staying in the Elasticsearch repo?**

* Reference content (anything that is or could be auto-generated): Settings, syntax references, functions lists…  
* Contribution guides and extending elasticsearch (clients, etc)  
* Release notes

### **Cloud**

#### **What has moved to elastic/docs-content?**

Public-facing narrative and conceptual docs have moved. Most can now be found under the following directories in the new docs:

* deploy-manage: deploying cloud, managing your cloud organization  
* troubleshooting: troubleshooting pages

#### **What is staying in the Cloud repo?**

* Reference content (= anything that is or could be auto-generated): Settings, syntax references, functions lists…  
* Release notes

### **Machine learning**

#### **What has moved to elastic/docs-content?**

Public-facing narrative and conceptual docs have moved. Most can now be found under the following directories in the new docs:

* deploy-manage: deploying cloud, managing your cloud organization  
* troubleshooting: troubleshooting pages

#### **What is staying in the Elasticsearch repo?**

* Reference content (= anything that is or could be auto-generated): Settings, syntax references, functions lists…  
* Release notes

## **IA Sections to product areas**

The following section shows a mapping of the different areas in the docs IA to the product areas and topics.

| **Area** | **Description** | **Sources (Area / Topic)** |
| ----- | ----- | ----- |
| **Get started** | Content in this section focuses on learning about Elasticsearch and the stack, learning about how it can be deployed, and basic environment setup for initial exploration. |  |
| **Solutions and use cases** | Content in this section focuses on the core solutions themselves and their user cases. | **Search:** core search solution content, playground, hybrid search **Observability:** core observability content **Security:** core security content  |
| **Manage data** | Content in this section focuses on learning about Elastic data store primitives, evaluating and implementing ingestion and data enrichment technologies, managing the lifecycle of your data and migrating data between clusters. | **Elasticsearch:** data store, migration, data lifecycle **Ingest:** ingesting data into Elasticsearch **Kibana**: Discover, Dashboards, Visualizations, Reporting, Alerting, dev tools. |
| **Explore and analyze** | Content in this section focusing on querying, visualizing, and exploring data. Additionally it focusing on levering machine learning for data analysis and defining and responding to alerts |  **Elasticsearch:**  query languages, scripting, machine learning (NLP)  **Machine Learning:** Anomaly detection, data frame analytics, NLP **Kibana**: Discover, Dashboards, Visualizations, Reporting, Alerting, dev tools. **ResponseOps:** Alerts and Cases |
| **Deploy and manage** | Content in this section focuses on evaluating deployment options and setting your environment up. This section also contains content around managing, securing, and monitoring your cluster.  | **Elasticsearch:** deploying elastic, production guidance, security, and Management |
| **Manage your Cloud account** | Content in this section focuses specifically on managing Cloud accounts. |  |
| **Troubleshoot** | Content in this section is troubleshooting content for the different products as well as how to contact support. | **All troubleshooting content** |
| **Extend and contribute** | Content in this section focuses on contributing to Elastic products and building plugins, clients, and beats modules. | **Contributions guides for:** Kibana Logstash Beats **Developer guides for:** Integrations Elasticsearch Plugins |
| **Reference** | Content in this section focuses on manuals for optional products as well as reference materials like query language references, configuration references, etc. | **Elastic APIS Elastic Clients Query Languages APM Agents Kibana:** Reference content, Release notes, Developer guide |

