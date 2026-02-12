# Troubleshooting content type guidelines

Source: https://www.elastic.co/docs/contribute-docs/content-types/troubleshooting

## What is a troubleshooting page

Troubleshooting pages help users fix specific problems they encounter while using Elastic products. They are intentionally narrow in scope (one primary issue per page), problem-driven, and focused on unblocking users as quickly as possible.

Use this content type when: users encounter a specific, repeatable problem; the problem can be identified through common symptoms; there is a known resolution or recommended workaround.

## Types of troubleshooting content

- **Dedicated troubleshooting page (preferred):** For specific, well-defined problems. Problem appears in the title.
- **Generic index page:** Organizes multiple related troubleshooting topics with links.
- **Troubleshooting section within a general page:** Use sparingly for brief, contextual troubleshooting.

## Required elements checklist

- [ ] **Filename:** Succinctly describes the problem (e.g., `no-data-in-kibana.md`, `traces-dropped.md`).
- [ ] **Frontmatter `applies_to`:** Tags for versioning/availability info.
- [ ] **Frontmatter `description`:** Describes the user-visible problem, suitable for search results.
- [ ] **Frontmatter `products`:** Relevant Elastic product(s).
- [ ] **Title:** Brief description of the problem from the user's perspective (e.g., "No application-level telemetry visible in Kibana").
- [ ] **Symptoms section:** Describes user-visible behavior. Uses bullet points. Focuses only on observable symptoms, not causes. Includes error messages or log output when helpful.
- [ ] **Resolution section:** Clear, actionable numbered steps. Prescriptive and opinionated. Steps ordered from most common to least common fix.

## Optional elements

- **Best practices section:** How to avoid the problem in the future. Preventive measures and configuration choices.
- **Resources section:** Links to supplementary documentation (not required to fix the issue).
- **Contact support section:** Only if no dedicated support page exists in the troubleshooting folder.
- **Diagnosis section:** When the symptom requires diagnostic steps before resolution. Placed before the Resolution section.

## Advanced resolution patterns

- **Multiple resolutions:** Present as a list of options when solutions can be applied independently or together.
- **Diagnostic branching:** Use a separate Diagnosis section when the same symptom has multiple root causes.
- **Deployment-specific resolutions:** Organize by deployment type with headings or tabs when steps differ significantly.

## Best practices

- Describe one primary issue per page.
- Be explicit about supported and unsupported setups.
- Optimize for fast resolution, not exhaustive coverage.
- Keep pages short and to the point.
- Be prescriptive: tell users exactly what to do.
- Order resolution steps from most common to least common.

## Anti-patterns to avoid

- Using troubleshooting to teach how to use a feature (use a tutorial).
- Explaining how a system works (use an overview).
- Listing configuration options or APIs (use reference documentation).
- Page titles "Troubleshooting X" with no specific problem (acceptable only for index pages).
- Long explanations before the resolution.
- Mixing multiple unrelated issues on one page.
- Speculative or diagnostic language in the resolution.
