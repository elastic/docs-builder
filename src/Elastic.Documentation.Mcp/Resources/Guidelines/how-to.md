# How-to guide content type guidelines

Source: https://www.elastic.co/docs/contribute-docs/content-types/how-tos

## What is a how-to guide

A how-to guide contains a short set of instructions to be carried out, in sequence, to accomplish a specific task. Think of it like a cooking recipe. It focuses on a single, self-contained task with minimal explanation.

How-to guides differ from tutorials: tutorials chain multiple how-to guides together with explanatory context. How-to guides are focused recipes for a single, discrete task.

## Required elements checklist

- [ ] **Filename:** Action verb pattern (e.g., `create-*.md`, `configure-*.md`, `run-elasticsearch-docker.md`).
- [ ] **Frontmatter `product`:** Relevant Elastic product(s).
- [ ] **Frontmatter `description`:** Brief summary fit for search results and tooltips.
- [ ] **Frontmatter `applies_to`:** Tags for versioning/availability info.
- [ ] **Title:** Precise description of the task using an action verb (e.g., "Run Elasticsearch in Docker").
- [ ] **Introduction:** Briefly explains what the guide helps accomplish and the expected outcome.
- [ ] **Before you begin section:** Lists permissions, data/configuration needs, and background knowledge links.
- [ ] **Steps:** Numbered instructions beginning with imperative verb phrases. Each step focused on a single action.
- [ ] **Success checkpoints:** Confirmation steps showing users whether critical actions succeeded.

## Recommended sections checklist

- [ ] **Related pages:** Links to conceptual topics, reference material, or other how-to guides.
- [ ] **Next steps:** Suggestions for what to do after completing the task.

## Optional elements

- Error handling: common errors and resolutions.
- Screenshots: for UI tasks when context is hard to describe (use sparingly).
- Code annotations: for important lines within code blocks.

## Best practices

- Use an ordered list for simple, linear steps.
- Use the stepper component for longer how-tos or complex steps.
- Show alternative approaches: use applies-switch for deployment-specific variations, tabs for UI vs API options.
- Test your steps: follow the instructions from start to finish to identify errors or unclear language.

## Anti-patterns to avoid

- Including extensive conceptual explanations (link to overviews instead).
- Combining multiple unrelated tasks in one guide.
- Skipping the "Before you begin" section.
- Writing steps that combine multiple actions.
- Omitting success checkpoints for critical actions.
