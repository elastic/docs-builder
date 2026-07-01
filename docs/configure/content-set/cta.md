---
navigation_title: CTA
cta: docs-builder
---

# CTA

The CTA (call-to-action) feature renders a card in the right-hand sidebar of a page, with a button and a short list of benefits. By default, every page shows the built-in `trial` card. Docsets can define their own named CTA templates and have individual pages opt into them.

## Define CTA templates

Add a `cta` map to your `docset.yml` file. Each key is a template name; the value defines the button and benefits.

```yaml
cta:
  mp:
    button:
      label: Get started on Elastic Cloud
      url: https://cloud.elastic.co/registration?page=docs&placement=docs-siderail
    benefits:
      - "14-day free trial"
      - "All features included"
```

- `button.label` and `button.url` are required.
- `benefits` is optional and limited to 3 entries.

You can also override the built-in default by defining your own `trial` entry — it replaces the default card sitewide for this docset.

## Select a CTA on a page

Use the `cta` frontmatter field to select a template by name:

```yaml
---
cta: mp
---
```

If a page omits `cta`, or names a template that doesn't exist in `docset.yml`, it falls back to the built-in `trial` CTA. An unknown name also emits a build warning.

## Click and impression tracking

CTA buttons are tracked via OpenTelemetry: a `cta_viewed` event fires the first time a card becomes visible, and a `cta_clicked` event fires on click. Both events carry the CTA's name, URL, label, and placement, so click-through rate can be compared across templates.
