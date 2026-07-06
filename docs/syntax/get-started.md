# Get started section

The first section of a hub-page body: a short onboarding funnel with an intro line and a set of numbered steps. A step can present two equally-weighted start **options** (for example, run locally versus try on Cloud), be a plain informational step, or link out as a whole card.

## Basic

```markdown
:::{get-started}
title: Get started in 3 steps
intro: Spin up Elasticsearch and Kibana, connect your data, and start exploring in minutes.
steps:
  - title: Run Elasticsearch and Kibana
    options:
      - label: Run locally
        description: Spin up Elasticsearch and Kibana on your machine for development.
        code: curl -fsSL https://elastic.co/start-local | sh
        language: sh
        url: /deploy-manage/deploy/self-managed/install-kibana
        url-label: Install self-managed
      - label: Try on Cloud
        description: Start a free Elastic Cloud trial. No local setup needed.
        url: https://cloud.elastic.co/registration
        url-label: Start a free trial
  - title: Connect your data
    description: Point Kibana at your data and create a data view.
    link: /manage-data/ingest
    link-label: Ingest data
  - title: Explore and visualize
    description: Open Discover, then build your first chart and dashboard.
    link: /explore-analyze/kibana-data-exploration-learning-tutorial
    link-label: Start the tutorial
:::
```

## Fields

| Field | Required | Description |
|---|---|---|
| `title` | yes | Section heading, for example `Get started in 3 steps`. |
| `intro` | no | One-line lead sentence. Rendered as inline markdown. |
| `steps[].title` | yes | Step heading, shown next to the step number. |
| `steps[].description` | no | Short supporting sentence. Rendered as inline markdown. |
| `steps[].link` | no | When set, the whole step is a clickable card linking here. |
| `steps[].link-label` | no | Call-to-action label for a link step. Defaults to `Open`. |
| `steps[].options[]` | no | Two or more equally-weighted start options, rendered side by side. |
| `steps[].options[].label` | no | Option heading, for example `Run locally`. |
| `steps[].options[].description` | no | Short supporting sentence. Rendered as inline markdown. |
| `steps[].options[].code` | no | A command shown in a copyable snippet (the "run locally" path). |
| `steps[].options[].language` | no | Syntax-highlighting language for the snippet (for example `sh`). |
| `steps[].options[].url` | no | Destination for the option. Rendered as a text link when the option also has `code`, or as a button otherwise. |
| `steps[].options[].url-label` | no | Label for the option link/button. |

An earlier top-level `install` / `tutorial` pair is still accepted for backward compatibility, but new pages should express the two start paths as `options` on the first step instead.

## Linking the hero to this section

The rendered section carries `id="get-started"`, so a [`{hero}`](hero.md) action can scroll to it instead of adding a competing entry point:

```markdown
:primary-action: [Get started](#get-started)
```

## When to use

`{get-started}` is the standard "front door" section between the [`{hero}`](hero.md) and the rest of the hub body. It's optional. Skip it when the hero already says what readers need to know next.

It is **not** an admonition. For caution / note / tip-style callouts inside body content, use the existing [admonitions](admonitions.md).
