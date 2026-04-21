# Agent skill

The `{agent-skill}` directive renders a standardized callout that points readers to an [Elastic AI agent skill](https://github.com/elastic/agent-skills). It includes a "Get the skill" button linking to the skill's URL.

## Usage

By default, the directive renders a standard description:

:::::{tab-set}

::::{tab-item} Output

:::{agent-skill}
:url: https://github.com/elastic/agent-skills@elasticsearch-esql
:::

::::

::::{tab-item} Markdown

```markdown
:::{agent-skill}
:url: https://github.com/elastic/agent-skills@elasticsearch-esql
:::
```

::::

:::::

You can also provide custom body text to clarify the scope of the skill:

:::::{tab-set}

::::{tab-item} Output

:::{agent-skill}
:url: https://github.com/elastic/agent-skills@elasticsearch-esql

This skill helps agents write and optimize ES|QL queries.
:::

::::

::::{tab-item} Markdown

```markdown
:::{agent-skill}
:url: https://github.com/elastic/agent-skills@elasticsearch-esql

This skill helps agents write and optimize ES|QL queries.
:::
```

::::

:::::

## Properties

| Property | Required | Description |
|----------|----------|-------------|
| `:url:`  | Yes      | Absolute URL to the skill on GitHub. |

The `:url:` property must be an absolute URL. Relative paths are not accepted, and the directive will emit a build error if the URL is missing or relative.
