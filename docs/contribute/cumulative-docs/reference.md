# Quick reference

The `applies_to` directive uses the following format:

```
<key>: <lifecycle> <version>
```

This page provides minimal reference information on the `applies_to` directive. For more detailed information, refer to [](/syntax/applies.md).

## key

:::{include} /_snippets/applies_to-key.md
:::

## lifecycle

:::{include} /_snippets/applies_to-lifecycle.md
:::

## version

:::{include} /_snippets/applies_to-version.md
:::


% ### Versioning facets
%
% There are multiple facets to versioning that we need to consider:
%
% | Facet | Description |
% | --- | --- |
% | **Stack flavor** | The Elasticsearch or Kibana feature base used for basic functionality. Either **Serverless** (sometimes "Elastic Cloud Serverless") or **{{stack}} <version>** |
% | **Deployment type** | The way Elastic is deployed:<br><br>- **Serverless** (sometimes "Elastic Cloud Serverless")<br>- **Elastic Cloud Hosted**<br>- **Elastic Cloud on Kubernetes**<br>- **Elastic Cloud Enterprise**<br>- **Self-managed**<br><br>All deployment types other than **Serverless** are used to run a **{{stack}} <version>** flavor of Elasticsearch / Kibana. ECK and ECE also have their own versioning. For example, one can run a v9.0.0 deployment on ECE 4.0.
% | **Project type** | The Serverless project types where a feature can be used - either **Elasticsearch**, **Elastic Security**, or **Elastic Observability** |
% | **Other versioning schemes** | Elastic products or tools with a versioned component, where stack versioning is not followed.<br><br>E.g. clients, Elastic Common Schema |
%
% % TODO: Final term for "Stack"
% % TODO: Final term for "Self-managed"
%
% **How many facets do I need to use?**
%
% The role of these labels is providing a trust signal that the reader is viewing content that’s applicable to them. This means that the relevant labels should appear on all pages. However, we can choose to expose only one versioning facet on pages where only one facet is relevant:
%
% * Depending on what you're documenting, you might need to include information from multiple facets. For example, when relevant features exist at both the stack and the deployment level, both of these groups might be used together (e.g. security, user management, and trust features differ both at the deployment level and at the stack version level).
%
% * In some sections, such as **Explore and analyze**, features generally only differ by stack flavor. In these cases, you can choose to include only this facet on the page.
