# Storybook

The `{storybook}` directive embeds a specific Storybook story iframe inside a docs page. It is intended for curated embeds from Storybooks hosted on Elastic-controlled paths, such as `/storybook/...`.

If your docset defines `storybook.root`, authors can omit `:root:` and provide only `:id:`.

## Usage

:::::{tab-set}

::::{tab-item} Output

:::{storybook}
:root: /storybook/kibana-eui
:id: components-button--primary
:height: 300
:title: Button / Primary story
:::

::::

::::{tab-item} Markdown

```markdown
:::{storybook}
:root: /storybook/kibana-eui
:id: components-button--primary
:height: 300
:title: Button / Primary story
:::
```

::::

:::::

## Properties

| Property | Required | Description |
|---|---|---|
| `:root:` | No* | Storybook root path or configured absolute URL, without `iframe.html`. Optional when `docset.yml` defines `storybook.root`. |
| `:id:` | Yes | Storybook story id, for example `components-button--primary`. |
| `:height:` | No | Height in pixels. Must be a positive integer. Defaults to `400`. |
| `:title:` | No | Accessible title for the iframe. Defaults to `Storybook story`. |

## Root validation

For safety, the directive accepts these `:root:` shapes:

- Root-relative URLs beginning with a single `/`, for example `/storybook/kibana-eui`
- Absolute `http://` or `https://` URLs that are explicitly configured in `docset.yml`

You can also use `/` when the Storybook lives at the literal root of the configured Storybook server.

The `:root:` value must not include `iframe.html`, a query string, or a fragment. The directive assembles the final iframe URL internally as:

```text
{root}/iframe.html?id={id}&viewMode=story
```

To allow an absolute root, add it to `docset.yml`:

```yaml
storybook:
  allowed_roots:
    - http://localhost:6006/storybook/kibana-eui
    - https://preview.example.com/storybook/kibana-eui
```

## Docset defaults

You can declare the default Storybook root at the docset level:

```yaml
storybook:
  root: /storybook/kibana-eui
```

Then authors only need `:id:`:

```markdown
:::{storybook}
:id: components-button--primary
:title: Button / Primary story
:::
```

If you want all root-relative Storybook paths in the docset to resolve against a specific Storybook server, add `server_root`:

```yaml
storybook:
  root: /storybook/kibana-eui
  server_root: http://localhost:6006
```

With that configuration, the directive resolves the final iframe URL as:

```text
http://localhost:6006/storybook/kibana-eui/iframe.html?id=components-button--primary&viewMode=story
```

## Optional body content

You can include Markdown content inside the directive. When present, it renders below the embedded story:

:::::{tab-set}

::::{tab-item} Output

:::{storybook}
:root: /storybook/kibana-eui
:id: components-button--primary
This example shows the default primary button treatment.
:::

::::

::::{tab-item} Markdown

```markdown
:::{storybook}
:root: /storybook/kibana-eui
:id: components-button--primary
This example shows the default primary button treatment.
:::
```

::::

:::::
