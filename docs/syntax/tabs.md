# Tabs

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


## Dropdown Tabs

Tab sets with many options — code samples across every client language, for example — wrap onto
several rows as a horizontal strip. Those render as a dropdown instead.

This is automatic for the `languages` group. Any other tab set can opt in or out with the
`dropdown` attribute:

```markdown
::::{tab-set}
:dropdown: true
:::{tab-item} Option one
Content for option one
:::
:::{tab-item} Option two
Content for option two
:::
::::
```

`:dropdown: false` forces a tab strip even for the `languages` group.

The tab strip is still present in the markup and becomes visible if JavaScript is unavailable,
so tab sets remain usable either way. Grouping and syncing (below) work the same in both renderings —
a dropdown and a tab strip in the same group stay in sync with each other.

### Example

::::{tab-set}
:dropdown: true
:::{tab-item} Option one
Content for option one
:::

:::{tab-item} Option two
Content for option two
:::

:::{tab-item} Option three
Content for option three
:::
::::

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
