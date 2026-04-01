# Kibana-Side Storybook MDX Transform

This note describes the missing piece between Kibana Storybook docs content and the `docs-builder` Storybook directive.

`docs-builder` already knows how to render this Markdown:

```markdown
:::{storybook}
:root: /storybook/kibana-eui
:id: components-button--primary
:title: Button / Primary story
:::
```

What it does not do is parse Storybook MDX directly. That work needs to happen in Kibana before content is handed to `docs-builder`.

## Goal

Convert supported Storybook MDX usage into plain Markdown that `docs-builder` can ingest.

The transform should turn this:

```mdx
import * as ButtonStories from './button.stories';
import { Meta, Story } from '@storybook/blocks';

<Meta of={ButtonStories} />

# Button

<Story of={ButtonStories.Primary} />
```

into this:

```markdown
# Button

:::{storybook}
:root: /storybook/kbn-ui
:id: components-button--primary
:title: Button / Primary story
:::
```

## Current `docs-builder` Contract

The directive contract is:

```markdown
:::{storybook}
:root: /storybook/<library>
:id: <storybook-story-id>
:height: 320
:title: Optional accessible title
:::
```

`docs-builder` assembles the iframe URL internally as:

```text
{root}/iframe.html?id={id}&viewMode=story
```

For absolute roots, the destination docs set must allow them in `docset.yml`:

```yaml
storybook:
  allowed_roots:
    - http://localhost:6006/storybook/kbn-ui
```

## Recommended Transform Scope

Support these shapes first:

1. `<Story of={Stories.Export} />`
2. `<Canvas of={Stories.Export} />`
3. `<Canvas><Story of={Stories.Export} /></Canvas>`
4. `<Meta of={Stories} />` only as metadata input, not as emitted output

Do not support these in v1:

1. Inline story definitions inside MDX
2. Arbitrary JSX inside `<Canvas>`
3. Stories that require executing React to discover metadata
4. Non-static `of={...}` expressions

## Transform Inputs

For each MDX file, the transform needs:

1. The MDX AST
2. The import map for referenced stories modules
3. Access to the referenced CSF file or its exported metadata
4. A mapping from the Kibana package or docs area to the Storybook root

## Transform Outputs

The transform should output a normal Markdown document that:

1. Preserves headings, prose, lists, and basic Markdown content
2. Replaces supported Storybook JSX blocks with `{storybook}` directives
3. Drops `<Meta>` from the output
4. Keeps the page readable without any Storybook-specific JSX left over

## Story Resolution

Given:

```mdx
import * as ButtonStories from './button.stories';

<Story of={ButtonStories.Primary} />
```

the transform should:

1. Resolve `ButtonStories` to `./button.stories`
2. Resolve `Primary` as the named export
3. Read the CSF meta title from the stories module
4. Build the canonical Storybook story id from `meta.title` and the export name
5. Emit the `{storybook}` directive

The important detail is that the transform should use Storybook's own story-id rules, not a hand-rolled approximation.

## Root Resolution

The transform should not hardcode a full iframe URL into Markdown.

Instead it should resolve a Storybook root for the current Kibana area, for example:

```text
src/platform/kbn-ui/... -> /storybook/kbn-ui
src/platform/packages/shared/... -> /storybook/shared-ux
```

A simple starting point is a checked-in map, for example:

```ts
const storybookRoots = {
  'src/platform/kbn-ui': '/storybook/kbn-ui',
  'src/platform/packages/shared': '/storybook/shared-ux',
};
```

The transform should pick the most specific matching prefix and emit that value as `:root:`.

## Title Resolution

Use a friendly iframe title when possible.

Recommended title format:

```text
<story title> / <export name>
```

Example:

```text
Button / Primary
```

If a friendly title cannot be derived safely, omit `:title:` and let `docs-builder` use its default.

## Suggested Algorithm

1. Parse the MDX file into an AST.
2. Record imports for stories modules.
3. Walk the AST.
4. Preserve normal Markdown nodes as Markdown.
5. For each supported Storybook JSX node:
   1. Resolve the referenced stories module and export.
   2. Read CSF metadata.
   3. Compute the canonical Storybook story id.
   4. Resolve the Storybook root for the current file.
   5. Emit a `{storybook}` directive block.
6. Drop `<Meta>` from output after using it for resolution.
7. Serialize the transformed document to Markdown.

## Example Transform Rules

### `<Story of={Stories.Primary} />`

Input:

```mdx
<Story of={ButtonStories.Primary} />
```

Output:

```markdown
:::{storybook}
:root: /storybook/kbn-ui
:id: components-button--primary
:title: Button / Primary
:::
```

### `<Canvas of={Stories.Primary} />`

Input:

```mdx
<Canvas of={ButtonStories.Primary} />
```

Output:

```markdown
:::{storybook}
:root: /storybook/kbn-ui
:id: components-button--primary
:title: Button / Primary
:::
```

### `<Canvas><Story of={Stories.Primary} /></Canvas>`

Input:

```mdx
<Canvas>
  <Story of={ButtonStories.Primary} />
</Canvas>
```

Output:

```markdown
:::{storybook}
:root: /storybook/kbn-ui
:id: components-button--primary
:title: Button / Primary
:::
```

## Error Handling

If the transform cannot safely resolve a story reference, it should fail with a precise message that includes:

1. The MDX file path
2. The JSX node that could not be resolved
3. The import or export name that failed

Examples:

```text
Could not resolve Story reference ButtonStories.Primary in src/platform/kbn-ui/docs/index.mdx
```

```text
Could not determine Storybook root for src/platform/packages/foo/docs/index.mdx
```

## Validation Checklist

The generated Markdown is correct if:

1. Every emitted story block uses `:root:` and `:id:`
2. `:root:` does not include `iframe.html`
3. `:root:` does not include query parameters
4. `:id:` matches Storybook's canonical story id
5. The rendered iframe URL becomes `{root}/iframe.html?id={id}&viewMode=story`

## Suggested Smoke Test

1. Pick one simple Storybook MDX page in Kibana.
2. Run the transform and inspect the generated Markdown.
3. Confirm the output contains one or more `{storybook}` blocks.
4. Point `docs-builder serve` at the generated docs folder.
5. Confirm the story renders in the docs page.

## Minimal Acceptance Bar

This work is complete enough for review when:

1. A Kibana Storybook MDX file containing `<Meta>` and `<Story of={...} />` transforms successfully
2. The transformed page renders in `docs-builder`
3. The embedded story loads using the configured Storybook root
4. The generated iframe URL includes `viewMode=story`
