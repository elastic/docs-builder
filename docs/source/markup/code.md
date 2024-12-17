---
title: Code
---

You can use the regular markdown code block:

```yaml
project:
  title: MyST Markdown
  github: https://github.com/jupyter-book/mystmd
  license:
    code: MIT
    content: CC-BY-4.0
  subject: MyST Markdown
```

But you can also use the [code directive](https://mystmd.org/guide/code) that supposedly give you more features.

```{code} yaml
project:
  title: MyST Markdown
  github: https://github.com/jupyter-book/mystmd
  license:
    code: MIT
    content: CC-BY-4.0
  subject: MyST Markdown
```

This page also documents the [code directive](https://mystmd.org/guide/directives). It mentions `code-block` and `sourcecode` as aliases of the `code` directive. But `code-block` seems to behave differently. For example the `caption` option works for `code-block`, but not for `code`.

```{code-block} yaml
:linenos:
:caption: How to configure `license` of a project
:name: myst.yml
:emphasize-lines: 4, 5, 6
project:
  title: MyST Markdown
  github: https://github.com/jupyter-book/mystmd
  license:
    code: MIT
    content: CC-BY-4.0
  subject: MyST Markdown
```

```{code-block} python
    :caption: Code blocks can also have sidebars.
    :linenos:

    print("one")
    print("two")
    print("three")
    print("four")
    print("five")
    print("six")
    print("seven")
```
