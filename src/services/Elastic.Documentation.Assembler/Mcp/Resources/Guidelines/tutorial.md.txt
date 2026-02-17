# Tutorial content type guidelines

Source: https://www.elastic.co/docs/contribute-docs/content-types/tutorials

## What is a tutorial

A tutorial is a comprehensive, hands-on learning experience that guides users through completing a meaningful task from start to finish. Think of it as a chain of related how-to guides, with additional explanatory context.

Tutorials differ from how-to guides in scope and purpose: tutorials chain multiple tasks together with explanatory context and code annotations. How-to guides are focused recipes for a single, discrete task.

Tutorials include three essential components: a sequence of instructional steps, prerequisites and setup, and clear learning objectives.

## Required elements checklist

- [ ] **Filename:** Descriptive pattern like `*-tutorial.md` (e.g., `ingest-pipeline-tutorial.md`).
- [ ] **Frontmatter `product`:** Relevant Elastic product(s).
- [ ] **Frontmatter `description`:** Brief summary of what users will learn.
- [ ] **Frontmatter `applies_to`:** Tags for versioning/availability info.
- [ ] **Title:** Descriptive title indicating what users learn or accomplish (e.g., "Build an ingest pipeline with processors").
- [ ] **Overview/introduction:** Explains what the tutorial teaches, who it's for, and what users will accomplish. Includes learning objectives as a bulleted list, intended audience, and skill level.
- [ ] **Before you begin section:** Lists all prerequisites: data sets, environments, software, hardware, access requirements, and prior knowledge.
- [ ] **Instructional steps:** Organized into logical sections with descriptive headings. Numbered steps beginning with imperative verbs.
- [ ] **Checkpoints and results:** After significant steps, shows what users should see or system state.
- [ ] **Code annotations:** Explains important lines within code blocks.
- [ ] **Next steps:** Follow-up tutorials, related features, or expansion suggestions.
- [ ] **Related pages:** Links to related documentation, blogs, or resources.

## Optional elements

- Summary: recap of what users learned, reinforcing key objectives.
- Time estimates: for each section or the overall tutorial.
- Explanatory callouts: admonitions for extra context or troubleshooting tips.
- Screenshots: for UI-based steps (use sparingly).

## Best practices

- Choose a tutorial approach: scenario-driven (real-world use case) or feature-focused (deep dive into features).
- Test the entire tutorial from scratch to identify gaps and unclear instructions.
- Have someone unfamiliar with the feature try the tutorial.
- Balance background context with readability. If too much context is needed, consider multiple focused tutorials.
- Ideally, users should complete the tutorial without needing to jump to other guides.

## Anti-patterns to avoid

- Making a tutorial that is really just a how-to guide (single task, no learning objective).
- Requiring users to constantly jump to other pages for essential information.
- Omitting learning objectives or audience description.
- Skipping checkpoints after significant steps.
- Not testing the tutorial end-to-end.
