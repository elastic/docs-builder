---
title: Links
---

A link contains link text (the visible text) and a link destination (the URI that is the link destination). The link text can include inline elements.

## Inline link

```
[Link title](links.md)
```

[Link title](links.md)

```
[**Hi**, _I'm md_](links.md)
```

[**Hi**, _I'm md_](links.md)

## Anchor link

You can link to a heading on a page with an anchor link. The link destination should be a `#` followed by the header text. Convert spaces to dashes (`-`).

```
I link to the [Inline link](#inline-link) heading above.
```

I link to the [Inline link](#inline-link) heading above.

```
I link to the [Notes](tables.md#notes) heading on the [Tables](tables.md) page.
```