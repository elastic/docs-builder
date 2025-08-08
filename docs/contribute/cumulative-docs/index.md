# Write cumulative documentation

% Audience: Anyone who contributes to docs or is curious about the cumulative documentation model
% Goals:
%   * Introduce the concept of cumulative documentation
%   * Explain its impacts on both readers and contributors
%   * Point docs contributors to more detailed resources on how to write cumulative documentation

In [elastic.co/docs](https://elastic.co/docs) (Docs V3), we write docs cumulatively regardless of the [branching strategy](/contribute/branching-strategy.md) selected. This means that in our Markdown-based docs, there is no longer a new documentation set published with every minor release: the same page stays valid over time and shows version-related evolutions.

:::{note}
This new behavior starts with the following **versions** of our products: Elastic Stack 9.0, ECE 4.0, ECK 3.0, and even more like EDOT docs. It also includes our unversioned products: Serverless and Elastic Cloud.

Nothing changes for our ASCIIDoc-based documentation system, that remains published and maintained for the following versions: Elastic Stack until 8.x, ECE until 3.x, ECK until 2.x, etc.
:::

## Reader experience

With cumulative documentation, when a user arrives in our documentation from an outside source, they land on a page that is a single source of truth. This means it will be more likely that the page they land on contains content that applies to them regardless of which version or deployment type they are using.

Users can then compare and contrast differences on a single page to understand what is available to them and explore the ways certain offerings might improve their experience.

## Contributor experience

With cumulative documentation, there is a single "source of truth" for each feature, which helps us to maintain consistency, accuracy, and maintainability of our documentation over time, and avoids "drift" between multiple similar sets of documentation.

As new minor versions are released, we want users to be able to distinguish which content applies to their own ecosystem and product versions without having to switch between different versions of a page.

This extends to deprecations and removals: No information should be removed for supported product versions, unless it was never accurate. It can be refactored to improve clarity and flow, or to accommodate information for additional products, deployment types, and versions as needed.

### When to tag content

To achieve this, the Markdown source files integrate a tagging system meant to identify:

* When content applies to or functions differently between **products or deployment types**.
* When features are introduced, modified, or removed in specific **versions** including lifecycle changes.

This tagging system is mandatory for all of the public-facing documentation. We refer to it as `applies_to` tags or badges.

**For detailed guidance on contributing to cumulative docs, refer to [](/contribute/cumulative-docs/best-practices.md).**

### When _not_ to tag content

* **Content-only changes**: Don't tag typos, formatting, or IA changes.
* **Every paragraph/section**: Only tag when the context or applicability changes from what has been established earlier on the page.
* **Unversioned products**: For products where all users are always on the latest version (like serverless), you don't need to tag workflow changes if the product lifecycle is unchanged.

### How dynamic tags work

We have a central version config called [`versions.yml`](https://github.com/elastic/docs-builder/blob/main/config/versions.yml), which tracks the latest released versions of our products. It also tracks the earliest version of each product documented in the Docs V3 system (the earliest available on elastic.co/docs). This central version config is used in certain inline version variables, and drives our [dynamic rendering logic](#how-do-these-tags-behave-in-the-output).

The logic allows us to label documentation related to unreleased versions as `planned`, continuously release documentation, and document our Serverless and {{stack}} offerings in one place.

:::{include} /contribute/_snippets/tag-processing.md
:::
