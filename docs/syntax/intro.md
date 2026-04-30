# Intro callout

A small white panel with a teal accent bar — used for "getting started" callouts directly under the hero.

## Basic

```markdown
:::{intro}
**New to Kibana?** [Take the tutorial](/tutorial) — a 30-minute, hands-on walk-through of Discover, dashboards, and ES|QL.
:::
```

Body is rendered as inline markdown. Use bold, links, and emphasis freely; keep it to one or two short paragraphs.

## When to use

`{intro}` is the standard "front door" panel between the [`{hero}`](hero.md) and the [`{on-this-page}`](on-this-page.md) chip. It's optional — skip it when the hero already says what readers need to know next.

It is **not** an admonition. For caution / note / tip-style callouts inside body content, use the existing [admonitions](admonitions.md).
