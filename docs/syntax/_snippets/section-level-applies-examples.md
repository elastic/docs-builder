* The whole page is generally applicable to {{stack}} 9.0 and to {{serverless-short}}, but one specific section isn’t applicable to {{serverless-short}} (and there is no alternative for it):

    ````markdown
    --- 
    applies_to:
      stack: ga
      serverless: ga
    ---

    # Spaces

    [...]

    ## Configure a space-level landing page [space-landing-page]
    ```{applies_to}
    stack: ga
    serverless: unavailable
    ```
    ````
    % I think we wanted to not specify stack here

* The whole page is generally applicable to all deployment types, but one specific paragraph only applies to {{ech}} and {{serverless-short}}, and another paragraph only applies to {{ece}}:

  ````md
  ## Cloud organization level security [cloud-organization-level]
  ```{applies_to}
  deployment:
    ess: ga
  serverless: ga
  ```

  [...]

  ## Orchestrator level security [orchestrator-level]
  ```{applies_to}
  deployment:
    ece: ga
  ```

  [...]
  ````