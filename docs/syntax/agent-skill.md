# Agent skill

The `{agent-skill}` directive renders a standardized callout that points readers to an [Elastic AI agent skill](https://github.com/elastic/agent-skills). When the URL includes a skill name (after `@`), it shows a "Copy install command" button that copies the `npx skills add @skill-name` command to clipboard. Otherwise, it falls back to a "Get the skill" link.

## Usage

By default, the directive renders a standard description with a copy button:

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
| `:url:`  | Yes      | Absolute URL to the skill. Include `@skill-name` to enable the copy install command button. |

The `:url:` property must be an absolute URL. Relative paths are not accepted, and the directive will emit a build error if the URL is missing or relative.

The skill name is extracted from the URL as the segment after `@`. For example, `https://github.com/elastic/agent-skills@elasticsearch-esql` produces the install command `npx skills add @elasticsearch-esql`.
