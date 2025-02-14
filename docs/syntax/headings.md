# Headings

You can have up to 6 levels of headings. But only levels 2 and 3 are displayed in table of contents sidebar.

## Basics

::::{tab-set}

:::{tab-item} Markdown

```markdown
# Heading 1
## Heading 2
### Heading 3
#### Heading 4
##### Heading 5
###### Heading 6

```

:::

:::{tab-item} HTML

```html
<div class="heading-wrapper" id="heading-1">
    <h1>
        <a class="headerlink" href="#heading-1">Heading 1</a>
    </h1>
</div>
```

:::

:::{tab-item} Output

# Heading 1

## Heading 2

### Heading 3

#### Heading 4

##### Heading 5

###### Heading 6

:::

::::

:::{note}

- Every page has to start with a level 1 heading.
- You should use only one level 1 heading per page.
- Headings inside directives like tabs or dropdowns causes the table of contents indicator to behave unexpectedly.
- If you are using the same heading text multiple times you should use a custom [anchor link](#anchor-links) to avoid conflicts.

:::

## Anchor Links

By default, the anchor links are generated based on the heading text. You can also specify a custom anchor link using the following syntax:

### Default Anchor Link

::::{tab-set}

:::{tab-item} Markdown

```markdown

## Hello-World

```

:::

:::{tab-item} Output

## Hello-World

:::

::::


### Custom Anchor Link

::::{tab-set}

:::{tab-item} Markdown

```markdown

## Heading [#custom-anchor]

```

:::

:::{tab-item} Output

## Heading [#custom-anchor]

:::

::::
