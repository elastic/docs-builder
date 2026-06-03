# Storybook

The `{storybook}` directive embeds a Storybook story from a Kibana `docs_registry.json`. The registry supplies the Storybook runtime ID, inline module entry, bootstrap assets, and iframe fallback URL.

Configure the registry URL in `docset.yml`:

```yaml
storybook:
  registry: https://example.com/storybook-docs/docs_registry.json
```

This page uses the Kibana Storybook artifact registry from PR 272388 so docs-builder preview builds can exercise inline module loading from `docs-v3-preview.elastic.dev`.

For local Kibana testing, `yarn storybook_docs shared_ux --serve` serves the registry at:

```text
http://127.0.0.1:6007/storybook-docs/docs_registry.json
```

## Usage

Use a registry ID directly:

```markdown
:::{storybook}
:id: kibana:shared_ux:ai-components-aibutton--default
:height: 300
:title: AI button default story
:::
```

:::{storybook}
:id: kibana:shared_ux:ai-components-aibutton--default
:height: 300
:title: AI button default story
:::

Or use structured properties:

```markdown
:::{storybook}
:project: kibana
:storybook: shared_ux
:component: ai-components-aibutton
:story: default
:::
```

:::{storybook}
:project: kibana
:storybook: shared_ux
:component: ai-components-aibutton
:story: default
:::

If a bare `:id:` is used, it must match exactly one story in the configured registry:

```markdown
:::{storybook}
:id: ai-components-aibutton--default
:::
```

In the PR 272388 registry this bare ID is ambiguous — it resolves to both `kibana:presentation:ai-components-aibutton--default` and `kibana:shared_ux:ai-components-aibutton--default` — so it is not rendered live here. Use the fully-qualified `project:storybook:docsId` form when a docs ID is not unique.

## Properties

| Property | Required | Description |
|---|---|---|
| `:id:` | Yes* | Full registry ID such as `kibana:shared_ux:ai-components-aibutton--default`, or a bare docs/storybook ID that matches exactly one configured story. |
| `:project:` | Yes* | Registry project prefix, for example `kibana`. Required when `:id:` is omitted. |
| `:storybook:` | Yes* | Storybook alias, for example `shared_ux`. Required when `:id:` is omitted. |
| `:component:` | No | Component ID used with `:story:` to form `{component}--{story}`. |
| `:story:` | Yes* | Story name or docs ID. Required when `:id:` is omitted. |
| `:height:` | No | Iframe fallback height in pixels. Defaults to the registry story height when present, otherwise `400`. |
| `:title:` | No | Accessible title for the iframe fallback. Defaults to `Storybook story`. |

## Rendering

If the registry story has `renderMode: inline` and an `inline` entry, docs-builder renders a `<storybook-story>` element. The browser loads the registry bootstrap styles and scripts, then imports `inline.entry` and calls `mountStory(story.storybookId, container)`.

If the story has `renderMode: iframe`, or no inline entry, docs-builder renders `iframe.url`.

The registry, inline module, bootstrap assets, and iframe fallback can live on different paths. docs-builder uses the URLs from the registry directly; those assets must allow browser access from the docs site.
