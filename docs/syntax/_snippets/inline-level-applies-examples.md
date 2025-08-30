::::::{dropdown} Basic example

:::::{tab-set}

::::{tab-item} Output

**Spaces** let you organize your content and users according to your needs.

- Each space has its own saved objects.
- {applies_to}`serverless: unavailable` Each space has its own navigation, called solution view.

::::

::::{tab-item} Markdown
```markdown
**Spaces** let you organize your content and users according to your needs.

- Each space has its own saved objects.
- {applies_to}`serverless: unavailable` Each space has its own navigation, called solution view.
```
::::

:::::

::::::

::::::{dropdown} Product-specific applicability with version information

:::::{tab-set}

::::{tab-item} Output

- {applies_to}`edot_python: preview 1.7.0`
- {applies_to}`apm_agent_java: beta 1.0.0`
- {applies_to}`elasticsearch: ga 9.1.0`

::::

::::{tab-item} Markdown
```markdown
- {applies_to}`edot_python: preview 1.7.0`
- {applies_to}`apm_agent_java: beta 1.0.0`
- {applies_to}`elasticsearch: ga 9.1.0`
```
::::

:::::

::::::

::::::{dropdown} Multiple products and states in a single inline statement

:::::{tab-set}

::::{tab-item} Output

- {applies_to}`{ serverless: "ga" , stack: "ga 9.1.0" }`
- {applies_to}`{ edot_python: "preview 1.7.0" , apm_agent_java: "beta 1.0.0" }`
- {applies_to}`{ stack: "ga 9.0" , deployment: { eck: "ga 9.0" } }`

::::

::::{tab-item} Markdown
```markdown
- {applies_to}`serverless: ga` {applies_to}`stack: ga 9.1.0`
- {applies_to}`edot_python: preview 1.7.0, ga 1.8.0` {applies_to}`apm_agent_java: beta 1.0.0, ga 1.2.0`
- {applies_to}`stack: ga 9.0` {applies_to}`eck: ga 3.0`
```
::::

:::::

::::::

::::::{dropdown} Complex scenarios with different product types

:::::{tab-set}

::::{tab-item} Output

- {applies_to}`{ stack: "preview 9.1", product: { edot_python: "preview 1.4.0" } }`

Notice that, when mixing different statement types, the applies_to inline syntax requires JSON-like nesting for product-specific applicability.

::::

::::{tab-item} Markdown
```markdown
- {applies_to}`{ stack: "preview 9.1", product: { edot_python: "preview 1.4.0" } }`
```

Notice that, when mixing different statement types, the applies_to inline syntax requires JSON-like nesting for product-specific applicability.

::::

:::::

::::::
