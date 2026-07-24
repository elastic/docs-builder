---
navigation_title: CTA
cta:
  id: docs-builder
---

# CTA

The CTA (call-to-action) feature renders a card in the right-hand sidebar of a page, with a button and a short list of benefits. By default, every page shows the built-in `trial` card. Docsets can define their own named CTA templates and have individual pages opt into them.

## Define CTA templates

Add a `cta` map to your `docset.yml` file. Each key is a template name; the value defines the button and benefits.

```yaml
cta:
  beta:
    button:
      label: Join the private beta
      url: https://example.com/beta-signup
    benefits:
      - "Early access to new features"
      - "Direct line to the team"
      - "Free for beta participants"
```

- `button.label` and `button.url` are required.
- `benefits` is optional and limited to 3 entries.

You can also override the built-in default by defining your own `trial` entry — it replaces the default card sitewide for this docset.

## Select a CTA on a page

Use the `cta` frontmatter field to select a template by `id`:

```yaml
---
cta:
  id: beta
---
```

If a page omits `cta`, the template scoped to its path (if any) applies; otherwise it falls back to the built-in `trial` CTA. An unknown `id` emits a build warning and is ignored.

## Scope a CTA to a path

To apply a template to every page under a directory without editing each file, list path prefixes under `paths`:

```yaml
cta:
  observability:
    button:
      label: Get started free
      url: https://cloud.elastic.co/serverless-registration?onboarding_token=observability
    benefits:
      - "14-day free trial"
    paths:
      - solutions/observability
```

- Paths are relative to the docset root (the directory containing `docset.yml`) and match whole path segments: `solutions/observability` covers `solutions/observability/apps/apm.md` but not `solutions/observability-labs/index.md`.
- When a page falls under more than one scoped path, the most specific (longest) prefix wins.
- A page's `cta` frontmatter always takes precedence over a path scope.
- Each path can only be claimed by one template; declaring the same path in two templates is a build error.

## Click and impression tracking

CTA buttons are tracked via OpenTelemetry: a `cta_viewed` event fires the first time a card becomes visible, and a `cta_clicked` event fires on click. Both events carry the CTA's name, URL, label, and placement, so click-through rate can be compared across templates.
