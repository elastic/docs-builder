---
title: Tabs
---

Tabbed content is created using the `tab-set` directive with individual `tab-item` blocks for each tab's content. You can embed other directives, like admonitions directly in tabs.

## Tabs Simple

### Example

#### Syntax

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

#### Example

::::{tab-set}

:::{tab-item} Tab #1 title
This is where the content for tab #1 goes.
:::

:::{tab-item} Tab #2 title
This is where the content for tab #2 goes.
:::

::::

---

## Tab Groups

Tabs can be grouped together by setting the `group` attribute to the same value for each `tab-set`.
This allows for multiple sets of tabs to be controlled together.

You need to set both the `group` and `sync` attributes to the same value for each `tab-item` to sync the tabs.

This means tabs with the same group and the same sync value will be selected together.

### Example

In the following example we have three tab sets, but only the first two are grouped together.
Hence, the first two tab sets will be in sync, but the third tab set will not be in sync with the first two.

#### Syntax
```markdown
::::{tab-set}
:group: languages // This is the group name
:::{tab-item} Java
:sync: java // This is the sync name
Content for Java tab
:::

:::{tab-item} Golang
:sync: golang
Content for Golang tab
:::

:::{tab-item} C#
:sync: csharp
Content for C# tab
:::

::::

::::{tab-set}
:group: languages
:::{tab-item} Java
:sync: java
Content for Java tab
:::

:::{tab-item} Golang
:sync: golang
Content for Golang tab
:::

:::{tab-item} C#
:sync: csharp
Content for C# tab
:::

::::
```

#### Result

##### Grouped Tabs

::::{tab-set}
:group: languages
:::{tab-item} Java
:sync: java
Content for Java tab
:::

:::{tab-item} Golang
:sync: golang
Content for Golang tab
:::

:::{tab-item} C#
:sync: csharp
Content for C# tab
:::

::::

::::{tab-set}
:group: languages
:::{tab-item} Java
:sync: java
Other content for Java tab
:::

:::{tab-item} Golang
:sync: golang
Other content for Golang tab
:::

:::{tab-item} C#
:sync: csharp
Other content for C# tab
:::
::::

##### Ungrouped Tabs

::::{tab-set}
:::{tab-item} Java
:sync: java
Other content for Java tab that is not in the same group
:::

:::{tab-item} Golang
:sync: golang
Other content for Golang tab that is not in the same group
:::

:::{tab-item} C#
:sync: csharp
Other content for Golang tab that is not in the same group
:::

::::

## Asciidoc syntax

`````asciidoc
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
`````
