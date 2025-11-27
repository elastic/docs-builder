# Footnotes

Footnotes allow you to add notes and references without cluttering the main text. They're automatically numbered and linked, providing an elegant way to include supplementary information, citations, or explanations.

## Plain paragraph test

This is a plain footnote test[^plain]. No directives involved.

[^plain]: This footnote is in a plain paragraph, outside any directive.

## Basic footnotes

::::{tab-set}

:::{tab-item} Output

Here's a simple footnote[^1] and another one[^2].

You can also use named identifiers[^my-note] which can be more descriptive in your source files.

:::

:::{tab-item} Markdown

```markdown
Here's a simple footnote[^1] and another one[^2].

You can also use named identifiers[^my-note] which can be more descriptive in your source files.

[^1]: This is the first footnote.
[^2]: This is the second footnote.
[^my-note]: This footnote uses a named identifier instead of a number.
```

:::

::::

[^1]: This is the first footnote.
[^2]: This is the second footnote.
[^my-note]: This footnote uses a named identifier instead of a number.

## Multiple references

You can reference the same footnote multiple times throughout your document.

::::{tab-set}

:::{tab-item} Output

First reference to the concept[^concept]. Some more text here. 

...

Second reference to the same concept[^concept].

:::

:::{tab-item} Markdown

```markdown
First reference to the concept[^concept]. Some more text here. 

...

Second reference to the same concept[^concept].

[^concept]: This explains an important concept that's referenced multiple times.
```

:::

::::

[^concept]: This explains an important concept that's referenced multiple times.

## Complex footnotes

Footnotes can contain multiple paragraphs, lists, blockquotes, and code blocks. Subsequent content must be indented to be included in the footnote.

::::{tab-set}

:::{tab-item} Output

This has a complex footnote[^complex].

:::

:::{tab-item} Markdown

```markdown
This has a complex footnote[^complex].

[^complex]: This footnote has multiple elements.

    It has multiple paragraphs with detailed explanations.

    > This is a blockquote inside the footnote.
    > It can span multiple lines.

    - List item one
    - List item two
    - List item three

    You can even include code:

    ```python
    def example():
        return "Hello from footnote"
    ```
```

:::

::::

[^complex]: This footnote has multiple elements.

    It has multiple paragraphs with detailed explanations.

    > This is a blockquote inside the footnote.
    > It can span multiple lines.

    - List item one
    - List item two
    - List item three

    You can even include code:

    ```python
    def example():
        return "Hello from footnote"
    ```

## Footnote placement

Footnote definitions should be placed at the document level (not inside directives like tab-sets, admonitions, or other containers). Footnote references can be used anywhere in your document, including inside directives. The footnote content will always be rendered at the bottom of the page.

::::{tab-set}

:::{tab-item} Output

Here's text with a footnote[^early].

More content here, and another footnote[^late].

Even more content in between.

:::

:::{tab-item} Markdown

```markdown
Here's text with a footnote[^early].

[^early]: This footnote is defined right after the reference.

More content here, and another footnote[^late].

Even more content in between.

[^late]: This footnote is defined later in the document.
```

:::

::::

[^early]: This footnote is defined right after the reference.
[^late]: This footnote is defined later in the document.

## Best practices

### Use descriptive identifiers

While you can use simple numbers like `[^1]`, descriptive identifiers like `[^api-note]` make your source more maintainable.

### Keep footnotes focused

Each footnote should contain a single, focused piece of information. If you find yourself writing very long footnotes, consider whether that content belongs in the main text.

### Consider alternatives

Before adding footnotes, consider whether:
- The information is important enough to be in the main text.
- A link to external documentation would be more appropriate.
- An admonition (note, warning, etc.) would be clearer.

### Numbering

Footnotes are automatically numbered in order of first reference, regardless of the identifier you use in your source. This means `[^zebra]` appearing before `[^apple]` will be numbered as footnote 1.