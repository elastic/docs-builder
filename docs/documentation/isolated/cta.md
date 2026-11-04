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

If a page omits `cta`, the template registered as the default for its navigation file (if any) applies; otherwise it falls back to the built-in `trial` CTA. An unknown `id` emits a build warning and is ignored.

## Register a default CTA on a navigation file

To apply a template to every page listed in a `docset.yml` or nested `toc.yml` without editing each file, set `default_cta` to a template name declared in `docset.yml`:

```yaml
# solutions/observability/toc.yml
default_cta: observability
toc:
  - file: index.md
  - file: apps/apm.md
```

```yaml
# docset.yml
cta:
  observability:
    button:
      label: Get started free
      url: https://cloud.elastic.co/serverless-registration?onboarding_token=observability
    benefits:
      - "14-day free trial"
```

- `default_cta` is available on both `docset.yml` and nested `toc.yml` files.
- Pages inherit the nearest `default_cta` from their navigation file. A nested `toc.yml` can override the value from a parent navigation file.
- A page's `cta` frontmatter always takes precedence over a navigation default.
- Each page can only be registered with one default CTA; listing the same page twice with different defaults is a build error.

## Register a default CTA on a navigation entry

When sibling sections share one navigation file, set `default_cta` on an individual `file:` or `folder:` entry instead. The template applies to that entry and every page beneath it:

```yaml
# solutions/toc.yml
toc:
  - file: index.md
  - file: observability.md
    default_cta: observability
    children:
      - folder: observability
  - file: security.md
    default_cta: security
    children:
      - folder: security
```

Here `solutions/index.md` keeps the built-in `trial` CTA, while `observability.md` and everything under `solutions/observability/` use the `observability` template.

When several defaults apply to a page, the one declared closest to the page in the navigation tree wins. An entry's `default_cta` overrides the `default_cta` of the `toc.yml` that lists it, and a nested `toc.yml` inside that entry overrides the entry. A page's own `cta` frontmatter always comes first, and pages with no default anywhere use the built-in `trial` CTA.

## Click and impression tracking

CTA buttons are tracked via OpenTelemetry: a `cta_viewed` event fires the first time a card becomes visible, and a `cta_clicked` event fires on click. Both events carry the CTA's name, URL, label, and placement, so click-through rate can be compared across templates.
