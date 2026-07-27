# List sub-pages

The `{list-sub-pages}` directive renders a list of child pages for the current section. Use it in an index page to help readers discover the pages within that section.

## Usage

Given this file structure:

```
docs/
└── getting-started/
    ├── index.md      ← directive goes here
    ├── install.md
    ├── concepts.md
    └── next-steps.md
```

:::::{tab-set}

::::{tab-item} Output

- [Install](#usage) — Set up the product in your environment.
- [Key concepts](#usage) — Core ideas and terminology.
- [Next steps](#usage) — Where to go from here.

::::

::::{tab-item} Markdown

```markdown
:::{list-sub-pages}
:::
```

::::

:::::

## Behavior

- **In `index.md`**: Lists all sibling pages at the same level in the table of contents.
- **Without an index**: If the section has no `index.md`, lists siblings of the first page in the TOC.
- **Folder siblings**: When a sibling is a folder, the directive shows a link to that folder's index page (or first TOC item).
- **Descriptions**: If a sibling page has a `description` in its frontmatter, it is shown below the link.
