---
navigation_title: Production
---

# Production Environment

The production environment is the main environment that is used to serve the
Elastic documentation. The documentation is a static site built with [docs-builder](https://github.com/elastic/docs-builder).

|   |   |
|---|---|
| URL | [https://www.elastic.co/docs](https://www.elastic.co/docs) |
| Terraform resources | [https://github.com/elastic/docs-infra/tree/main/aws/elastic-web/us-east-1/elastic-docs-v3-prod](https://github.com/elastic/docs-infra/tree/main/aws/elastic-web/us-east-1/elastic-docs-v3-prod) |


## Architecture

The production environment is an S3 bucket served by CloudFront defined by the [elastic-docs-v3 terraform module](https://github.com/elastic/docs-infra/tree/main/modules/elastic-docs-v3).

However, infront of the CloudFront distribution, Fastly is used to serve the site. 
The reason is that Web team is using Fastly to serve the website and they are redirecting
the `/docs` path to our CloudFront distribution.

## Deployment

The production environment is deployed by the [Assembler Build and Deploy](https://github.com/elastic/docs-internal-workflows/actions/workflows/assembler-build.prod.yml)
workflow.
