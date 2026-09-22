---
navigation_title: Serverless releases
---

# Onboard serverless releases

Use this path when your product publishes from a date-based serverless promotion. Complete the [shared onboarding steps](onboard.md) first.

Serverless release notes do not use a GitHub release event. After a production promotion, the product pipeline starts the docs-owned `docs-release-notes` Buildkite pipeline. That pipeline finds the promoted Git range, builds a date-versioned bundle, and publishes it.

:::{important}
Serverless onboarding is not self-service yet. Contact `#docs-eng` before you add the promotion trigger. The docs team must configure the service mapping and confirm that the shared pipeline is ready for your product.
:::

When the docs team confirms the setup, add the `docs-release-notes` trigger to the production quality-gate pipeline. The trigger must run asynchronously and use `soft_fail`, so a documentation failure cannot block a product promotion.

The rollout and the current product integrations are tracked in [Serverless release-notes automation](https://github.com/elastic/docs-eng-team/issues/823).
