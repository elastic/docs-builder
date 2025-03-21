---
navigation_title: Preview
---

# Preview Environment

The preview environment is a shared environment that is used to test changes on
Pull Requests. Each Pull Request is deployed to a unique path in the preview
environment.

|   |   |
|---|---|
| URL | [https://docs-v3-preview.elastic.dev](https://docs-v3-preview.elastic.dev) |
| Terraform resources | [https://github.com/elastic/docs-infra/tree/main/aws/elastic-web/us-east-1/elastic-docs-v3-preview](https://github.com/elastic/docs-infra/tree/main/aws/elastic-web/us-east-1/elastic-docs-v3-preview) |


## Architecture

The preview environment is an S3 bucket served by CloudFront defined by the [elastic-docs-v3 terraform module](https://github.com/elastic/docs-infra/tree/main/modules/elastic-docs-v3).

## Deployment

Each repository has a GitHub Actions workflow that is triggered when a Pull Request is created.
For example the [docs-build.yml](https://github.com/elastic/elasticsearch/blob/main/.github/workflows/docs-build.yml) workflow on the [elastic/elasticsearch](https://github.com/elastic/elasticsearch) repository. At it's core it uses the [preview-build.yml](https://github.com/elastic/docs-builder/blob/main/.github/workflows/preview-build.yml) reusable workflow to build and deploy the preview environment.

## Security

We are using OIDC to authenticate the GitHub Actions workflow to AWS.
You can find a list of GitHub repositories that are allowed to deploy to AWS in the
[repositories.yml](https://github.com/elastic/docs-infra/blob/main/modules/aws-github-actions-oidc-roles/repositories.yml).

For each repository, we create a new IAM role that allows the GitHub Actions workflow
of that repository to deploy to a specific path in the S3 bucket.

E.g. The GitHub Actions workflow for the [elastic/kibana](https://github.com/elastic/kibana) repository is allowed to deploy to the `/elastic/kibana` path in the S3 bucket.
