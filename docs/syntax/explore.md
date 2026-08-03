# Explore

The **Explore {product}** section of a [hub page](hub-pages.md): a titled band that
houses a stack of collapsible **accordion groups**. It wraps one or more
[`{card-group}`](card-group.md) directives. Inside `{explore}`, each card-group
renders as an accordion and each [`{link-card}`](link-card.md) renders as a titled
link **column** instead of a bordered card.

The first accordion in the stack is expanded by default, and the rest are collapsed.
Toggling uses native `<details>`/`<summary>`, so it works without JavaScript.

## Basic

```markdown
:::::{explore}
:title: Explore Kibana
:intro: Explore the apps and capabilities that help you act on your data.

::::{card-group}
:title: Install & admin
:id: install

:::{link-card}
title: Self-managed
link: /deploy-manage/deploy/self-managed
links:
  - { label: Docker, url: /deploy/docker }
  - { label: Configure, url: /deploy/configure }
aside:
  label: Even more
  links:
    - { label: RPM, url: /deploy/rpm }
    - { label: Windows, url: /deploy/windows }
:::
::::

::::{card-group}
:title: Visualize & analyze
:id: visualize

:::{link-card}
title: Dashboards
link: /explore-analyze/dashboards
links:
  - { label: Create a dashboard, url: /dashboards/create }
:::
::::
:::::
```

The outer `{explore}` fence uses **five** colons so the four-colon `::::` card-group
fences and three-colon `:::` link-card fences nest without closing it early. Add more
colons on the outer fence if you need to nest deeper.

## Options

| Option | Notes |
|---|---|
| `:title:` | H2 heading for the section (for example, `Explore Kibana`). Required. |
| `:intro:` | Optional intro paragraph below the heading. |
| `:id:` | Section anchor. |

## How nested directives change

Inside `{explore}`, the same authoring you already use for card grids is reinterpreted:

| Directive | Standalone rendering | Inside `{explore}` |
|---|---|---|
| `{card-group}` | Section heading + card grid | A collapsible accordion group (title in the header, cards inside) |
| `{link-card}` | A bordered card | A titled link **column** (title and link list, with the `description` not shown in column mode) |
| `{link-card}` `aside` | Inline "label: A · B · C" line | An **"Even more"** badge cluster under the column |

Because the effect is driven by the `{explore}` ancestor, the card-group and link-card
bodies don't need any extra options. Wrapping them is enough.
