There are three typical scenarios to start from:

* The documentation set or page is primarily about using or interacting with Elastic Stack components or the Serverless UI:

    ```yml
    ---
    applies_to:
      stack: ga
      serverless: ga
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
  ---

  ```

* The documentation set or page is primarily about a product following its own versioning schema:

  ```yml
  ---
  applies_to:
    edot_ios: ga
  ---
  ```

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
---
```
% I don't know what this example is supposed to show




