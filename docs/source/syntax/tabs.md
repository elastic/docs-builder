---
title: Tabs
---

Tabbed content is created using the `tab-set` directive with individual `tab-item` blocks for each tab's content. You can embed other directives, like admonitions directly in tabs.

## Syntax

:::none
\:\:\:\:\{tab-set}

\:\:\:\{tab-item} Tab #1 title
This is where the content for tab #1 goes.
\:\:\:

\:\:\:\{tab-item} Tab #2 title
This is where the content for tab #2 goes.
\:\:\:

\:\:\:\:
:::

::::{tab-set}

:::{tab-item} Tab #1 title
This is where the content for tab #1 goes.
:::

:::{tab-item} Tab #2 title
This is where the content for tab #2 goes.
:::

::::

## Asciidoc syntax

:::none
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
:::