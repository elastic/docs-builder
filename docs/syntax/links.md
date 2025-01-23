# Links

A link contains link text (the visible text) and a link destination (the URI that is the link destination). The link text can include inline elements.

## Inline link

```markdown
[Link title](links.md)
```

[Link title](links.md)

```markdown
[**Hi**, _I'm md_](links.md)
```

[**Hi**, _I'm md_](links.md)

## Anchor link

You can link to a heading on a page with an anchor link. The link destination should be a `#` followed by the header text. Convert spaces to dashes (`-`).

```markdown
I link to the [Inline link](#inline-link) heading above.
```

I link to the [Inline link](#inline-link) heading above.

```markdown
I link to the [Notes](tables.md#notes) heading on the [Tables](tables.md) page.
```

## Cross Links

Cross links are links that point to a different docset.

```markdown
[Cross link](kibana://cross-link.md)
```

The syntax is `<scheme>://<path>`, where <scheme> is the repository name and <path> is the path to the file.

## Auto Text Links

If you link to a local markdown file but omit any link text `docs-builder` will use the target's [title](titles.md).

```markdown
[](applies.md)
```
will output: [](applies.md)

You can go one step further to auto generate link text for headings within files:

```markdown
[](applies.md#sections)
```

will output: [](applies.md#sections)

This also applies to local anchors


```markdown
[](#anchor-link)
```

will output: [](#anchor-link)

## Heading anchors

Headings will automatically create anchor links in the resulting html. 

```markdown
## This Is A Header
```

Will have an anchor link injected with the name `this-is-an-header`.


If you need more control over the anchor name you may specify it inline

```markdown
## This Is A Header [#but-this-is-my-anchor]
```

Will result in an anchor link named `but-this-my-anchor` to be injected instead. 

Do note that these inline anchors will be normalized. 

```markdown
## This Is A Header [What about this for an anchor!]
```

Will result in the anchor `what-about-this-for-an-anchor`.


## Inline anchors

Docsbuilder temporary supports the abbility to create a linkable anchor anywhere on any document. 

```markdown
This is text and $$$this-is-an-inline-anchor$$$
```

This feature exists to aid with migration however is scheduled for removal and new content should **NOT** utilize this feature.