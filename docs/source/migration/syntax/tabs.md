---
title: Tabs
---

This guide provides instructions for converting AsciiDoc tabbed widgets to MyST Markdown format.

## Tabbed Widget Mapping

In AsciiDoc, tabbed widgets are created using HTML passthrough blocks combined with `include::` statements for rendering content within each tab. In MyST Markdown, tabbed content is created using the `tab-set` directive with individual `tab-item` blocks for each tab's content.

### Example Conversion

In AsciiDoc:
```text
[[tabbed-widgets]]
== Tabbed widgets

Improve the usability of your docs by adding tabbed widgets.
These handy widgets let you conditionally display content based on the selected tab.

**`widget.asciidoc`**

[source,asciidoc]
----
++++
<div class="tabs" data-tab-group="custom-tab-group-name">
  <div role="tablist" aria-label="Human readable name of tab group">
    <button role="tab" aria-selected="true" aria-controls="cloud-tab-config-agent" id="cloud-config-agent">
      Tab #1 title
    </button>
    <button role="tab" aria-selected="false" aria-controls="self-managed-tab-config-agent" id="self-managed-config-agent" tabindex="-1">
      Tab #2 title
    </button>
  </div>
  <div tabindex="0" role="tabpanel" id="cloud-tab-config-agent" aria-labelledby="cloud-config-agent">
++++

// include::content.asciidoc[tag=central-config]

++++
  </div>
  <div tabindex="0" role="tabpanel" id="self-managed-tab-config-agent" aria-labelledby="self-managed-config-agent" hidden="">
++++

// include::content.asciidoc[tag=reg-config]

++++
  </div>
</div>
++++
----

**`content.asciidoc`**

[source,asciidoc]
----
// tag::central-config[]
This is where the content for tab #1 goes.
// end::central-config[]

// tag::reg-config[]
This is where the content for tab #2 goes.
// end::reg-config[]
----
```

In MyST Markdown:
```markdown
::::{tab-set}

:::{tab-item} Tab #1 title
This is where the content for tab #1 goes.
:::

:::{tab-item} Tab #2 title
This is where the content for tab #2 goes.
:::

::::
```

::::{tab-set}

:::{tab-item} Tab #1 title
This is where the content for tab #1 goes.
:::

:::{tab-item} Tab #2 title
This is where the content for tab #2 goes.
:::

::::

## Converting HTML and Passthrough Blocks

In MyST, tabbed widgets don’t require passthrough HTML blocks or custom IDs/attributes for accessibility, as the `tab-set` directive handles all tab behavior. Each `tab-item` block represents a single tab, with its label provided in the directive header.

### Example

In AsciiDoc:
```text
<div class="tabs" data-tab-group="example-group">
  <button role="tab" aria-controls="example-tab1">Tab 1</button>
  <button role="tab" aria-controls="example-tab2">Tab 2</button>
</div>
```

In MyST Markdown:
```markdown
::::{tab-set}

:::{tab-item} Tab 1
Content for Tab 1.
:::

:::{tab-item} Tab 2
Content for Tab 2.
:::

::::
```

::::{tab-set}

:::{tab-item} Tab 1
Content for Tab 1.
:::

:::{tab-item} Tab 2
Content for Tab 2.
:::

::::