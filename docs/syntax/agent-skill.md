# Agent skill

The `{agent-skill}` directive renders a standardized callout that points readers to an [Elastic AI agent skill](https://github.com/elastic/agent-skills). It uses a fixed title and description, and includes a "Get the skill" button linking to the skill's URL.

## Usage

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

## Properties

| Property | Required | Description |
|----------|----------|-------------|
| `:url:`  | Yes      | Absolute URL to the skill on GitHub. |

The `:url:` property must be an absolute URL. Relative paths are not accepted, and the directive will emit a build error if the URL is missing or relative.
