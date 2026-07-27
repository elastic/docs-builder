`applies_to` keys accept comma-separated values to specify lifecycle states for multiple product versions.

When you specify multiple lifecycles with simple versions, the system automatically infers whether each version represents an exact version, a range, or an open-ended range. Refer to [Implicit version inference](/_snippets/applies_to-version.md#implicit-version-inference) for details.

### Examples

* A feature is added in 9.0 as tech preview and becomes GA in 9.1: 

    ```yml
    applies_to:
      stack: preview 9.0, ga 9.1
    ```

    The preview is automatically interpreted as `=9.0` (exact), and GA as `9.1+` (open-ended).

* A feature goes through multiple stages before becoming GA:

    ```yml
    applies_to:
      stack: preview 9.0, beta 9.1, ga 9.3
    ```

    Interpreted as: `preview =9.0`, `beta 9.1-9.2`, `ga 9.3+`

* A feature is unavailable for one version, beta for another, preview for a range, then GA:

    ```yml
    applies_to:
      stack: unavailable 9.0, beta 9.1, preview 9.2, ga 9.4
    ```

    Interpreted as: `unavailable =9.0`, `beta =9.1`, `preview 9.2-9.3`, `ga 9.4+`

* A feature is deprecated in ECE 4.0 and is removed in 4.8. At the same time, it has already been removed in {{ech}}:

    ```yml
    applies_to:
      deployment:
        ece: deprecated 4.0, removed 4.8
        ess: removed
    ```

    The deprecated lifecycle is interpreted as `4.0-4.7` (range until removal).

* Use explicit specifiers when you need precise control:

    ```yml
    applies_to:
      # Explicit exact version
      stack: preview =9.0, ga 9.1+
      
      # Explicit range
      stack: beta 9.0-9.1, ga 9.2+
    ```