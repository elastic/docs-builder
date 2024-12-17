---
title: File Inclusion
myst:
    substitutions:
        'page_title': "File Inclusion Page"
---

You can include any markdown page using `include` directive. The snippets can also contain variables and be substituted as shown in the [Substitutions](substitutions.md) page. This is specially useful if pages need to customize the snippet. For that, pages just need to use the front matter to define their own values for the variables in the snippet.

The rest of this page is from a snippet and "{{page_title}}" below is taken from the front matter on this page.

## Snippet

```{include} _snippets/my_snippet.md
```
