* When a change is released in `ga`, users need to know which version the feature became available in:

    ```
    ---
    applies_to:
      stack: ga 9.3
    ---
    ```

    This means the feature is available from version 9.3 onwards (equivalent to `ga 9.3+`).

* When a change is introduced as preview or beta, use `preview` or `beta` as value for the corresponding key within the `applies_to`:

    ```
    ---
    applies_to:
      stack: beta 9.1
    ---
    ```

* When a feature is available only in a specific version range, use the range syntax:

    ```
    ---
    applies_to:
      stack: beta 9.0-9.1, ga 9.2
    ---
    ```

    This means the feature was in beta from 9.0 to 9.1, then became GA in 9.2+.

* When a feature was in a specific lifecycle for exactly one version, use the exact syntax:

    ```
    ---
    applies_to:
      stack: preview =9.0, ga 9.1
    ---
    ```

    This means the feature was in preview only in 9.0, then became GA in 9.1+.

* When a change introduces a deprecation, use `deprecated` as value for the corresponding key within the `applies_to`:

    ```
    ---
    applies_to:
      deployment:
        ece: deprecated 4.2
    ---
    ```

* When a change removes a feature, any user reading the page that may be using a version of Kibana prior to the removal must be aware that the feature is still available to them. For that reason, we do not remove the content, and instead mark the feature as removed:

    ```
    ---
    applies_to:
      stack: deprecated 9.1, removed 9.4
    ---
    ```

    With the implicit version inference, this is interpreted as `deprecated 9.1-9.3, removed 9.4+`.