---
navigation_title: Content set
---

# Content set configuration

Now we'll zoom into the AsciiDoc book level, and explore the V3 equivalent: content sets. At the book level, the system consists of:

| System property | Asciidoc | V3 |
| -------------------- | -------------------- | -------------------- |
| **Content source files** --> A whole bunch of markup files as well as any other assets used in the docs (for example, images, videos, and diagrams). | **Markup**: AsciiDoc files **Assets**: Images, videos, and diagrams | **Markup**: MD files **Assets**: Images, videos, and diagrams |
| **Information architecture** --> A way to specify the order in which these text-based files should appear in the information architecture of the book. | `index.asciidoc` file (this can be spread across several AsciiDoc files, but generally starts with the index file specified in the `conf.yaml` file)) | TBD |
