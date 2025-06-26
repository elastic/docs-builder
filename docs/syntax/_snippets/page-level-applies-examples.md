There are 3 typical scenarios to start from:

* The documentation set or page is primarily about using or interacting with Elastic Stack components or the Serverless UI:

    ```yml
    --- 
    applies_to:
      stack: ga
      serverless: ga
    products:
      - id: kibana
      - id: elasticsearch
      - id: elastic-stack
    ---
    ```

* The documentation set or page is primarily about orchestrating, deploying or configuring an installation (only include relevant keys):

  ```yml
  --- 
  applies_to:
    serverless: ga
    deployment: 
      ess: ga
      ece: ga
      eck: ga
  products:
    -id: cloud-serverless
    -id: cloud-hosted
    -id: cloud-enterprise
    -id: cloud-kubernetes
  ---

  ```

* The documentation set or page is primarily about a product following its own versioning schema:

  ```yml
  --- 
  applies_to:
    product: ga
  products:
    -id: edot-collector
  ---
  ```
  % changing soon

It can happen that it’s relevant to identify several or all of these dimensions for a page. Use your own judgement and check existing pages in similar contexts.

```yml
--- 
applies_to:
  stack: ga
  serverless: ga
  deployment: 
    ess: ga
    ece: ga
    eck: ga
products:
  -id: kibana
  -id: elasticsearch
  -id: elastic-stack
  -id: cloud-serverless
  -id: cloud-hosted
  -id: cloud-enterprise
  -id: cloud-kubernetes
---
```
% I don't know what this example is supposed to show




