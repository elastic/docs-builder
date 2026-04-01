# Storybook + Kibana Local Testing

This note outlines how to test the new `{storybook}` directive from a local `docs-builder` checkout while working inside the Kibana repo.

The current directive shape is:

```markdown
:::{storybook}
:root: /storybook/kibana-eui
:id: components-button--primary
:height: 320
:title: Button / Primary story
:::
```

`docs-builder` assembles the iframe URL internally as:

```text
{root}/iframe.html?id={id}&viewMode=story
```

## Goal

There are two things to validate:

1. A hand-written docs page in Kibana renders a Storybook embed correctly.
2. A transformed Storybook MDX file produces the expected Markdown and renders correctly in `docs-builder`.

## Prerequisites

- A local checkout of `elastic/docs-builder`
- A local checkout of `elastic/kibana`
- `.NET SDK 10`
- A Storybook root that the directive can accept:
  - A root-relative path such as `/storybook/kibana-eui`
  - Or an absolute root URL that is configured in `docset.yml`

## Option 1: Use `docs-builder` directly from source

From the `docs-builder` repo:

```bash
dotnet run --project src/tooling/docs-builder -- serve -p /absolute/path/to/kibana/storybook-docs-poc/docs
```

This is the fastest loop while iterating on directive behavior.

## Option 2: Build a local binary first

From the `docs-builder` repo:

```bash
dotnet build
```

Then use the local debug binary:

```bash
./.artifacts/bin/docs-builder/debug/docs-builder serve -p /absolute/path/to/kibana/storybook-docs-poc/docs
```

Use this when you want Kibana-side testing to be independent from the `docs-builder` build command.

## Create a tiny docs set inside Kibana

Create a temporary folder in Kibana, for example:

```text
kibana/storybook-docs-poc/docs/docset.yml
kibana/storybook-docs-poc/docs/index.md
```

Use this `docset.yml`:

```yaml
project: kibana
dev_docs: true
toc:
  - file: index.md
```

If you want authors to omit `:root:` in page content, declare a docset-level Storybook root:

```yaml
project: kibana
dev_docs: true
storybook:
  root: /storybook/kibana-eui
toc:
  - file: index.md
```

If you want to test an absolute Storybook root, add:

```yaml
storybook:
  root: /storybook/kibana-eui
  server_root: http://localhost:6006
  allowed_roots:
    - http://localhost:6006/storybook/kibana-eui
```

## Manual directive smoke test

Put this in `index.md`:

```markdown
# Storybook smoke test

:::{storybook}
:id: components-button--primary
:height: 320
:title: Button / Primary story
:::
```

Then run:

```bash
dotnet run --project /absolute/path/to/docs-builder/src/tooling/docs-builder -- serve -p /absolute/path/to/kibana/storybook-docs-poc/docs
```

Open:

```text
http://localhost:3000
```

Verify:

- The page builds without directive validation errors
- The generated page contains an iframe
- The iframe URL is `/storybook/kibana-eui/iframe.html?id=components-button--primary&viewMode=story`
- The embedded story renders

## Testing a transformed Storybook MDX file

The Kibana MDX transformer should emit Markdown in the new directive format.

For example, an MDX fragment like:

```mdx
import * as ButtonStories from './button.stories';

<Meta of={ButtonStories} />

# Button

<Story of={ButtonStories.Primary} />
```

should become something close to:

```markdown
# Button

:::{storybook}
:id: components-button--primary
:title: Button / Primary story
:::
```

### Suggested MDX validation loop

1. Pick one simple Storybook MDX file in Kibana.
2. Run the transformer to generate Markdown.
3. Write the generated Markdown into the temporary Kibana docs set.
4. Run `docs-builder serve` against that docs folder.
5. Compare the rendered page with the original Storybook page.

## How to make the Storybook root work locally

The directive does not accept arbitrary absolute roots unless they are configured in `docset.yml`.

For local testing, use one of these approaches:

### Preferred: same-origin proxy

Put a small reverse proxy in front of both services so:

- `/storybook/...` routes to the local Storybook server
- `/` routes to `docs-builder serve`

That preserves the production-style root-relative URL shape and avoids changing the generated Markdown.

### Alternative: use an absolute configured root

If you want to point directly at a local or hosted Storybook server, configure it in `docset.yml` first:

```yaml
project: kibana
dev_docs: true
storybook:
  allowed_roots:
    - http://localhost:6006/storybook/kibana-eui
    - https://preview.example.com/storybook/kibana-eui
toc:
  - file: index.md
```

Then point `:root:` there:

```markdown
:::{storybook}
:root: http://localhost:6006/storybook/kibana-eui
:id: components-button--primary
:::
```

This is the easiest way to validate the directive without setting up a local proxy.

## Recommended proxy shape

If you want a local end-to-end test that matches production behavior, serve:

- Docs at `http://localhost:3000/`
- Storybook at `http://localhost:3000/storybook/kibana-eui/`

The important detail is not the proxy technology. The important detail is that the final embed URL remains root-relative and same-origin from the docs page's point of view.

## What to check in the generated Markdown

The transformer output is correct if it:

- Uses `:root:` and `:id:` instead of a full iframe URL
- Or omits `:root:` when `docset.yml` already defines `storybook.root`
- Preserves the Storybook story id exactly
- Does not include `iframe.html` inside `:root:`
- Does not include query string data inside `:root:`
- Emits a reasonable `:title:` when one is available

## Quick acceptance checklist

- The temporary Kibana docs set builds locally with `docs-builder serve`
- A hand-written `{storybook}` block renders correctly
- A transformed MDX file emits the expected directive shape
- The embedded story loads in the docs page
- The local setup does not require changing the directive format for local vs hosted testing

## Example expected output

```markdown
# Button

:::{storybook}
:root: /storybook/kibana-eui
:id: components-button--primary
:height: 320
:title: Button / Primary story
:::
```
