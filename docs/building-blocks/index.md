---
navigation_title: Building Blocks
---

# Building Blocks

This section explains all the building blocks that are used to build the documentation.

## Documentation Set

A single folder containing the documentation of a single repository. At a minimum, this folder should contain:

* `docset.yml` file
* `index.md` file

See [docset.yml](../configure/content-set/index.md) for more configuration details.

## Assembled Documentation

The product of building many documentation sets and weaving them in to a global navigation producing the fully assembled documentation site.

See [site configuration](../configure/site/index.md) for more details on the actual configuration.

## Distributed documentation

The purpose of separating building documentation sets and assembling them is to allow for distributed builds.

Each build of documentation set produces a `links.json` (Link Index) file that contains all the linkable resources in the repository.
This `links.json` file is then published to a central location (Link Service) everytime a repository successfully builds on its respective default integration branch.

For example, [Elasticsearch's links.json](https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/elasticsearch/main/links.json) represents all linkable resources in the Elasticsearch repository.

This allows us to:

* Validate outbound and inbound links ahead of time, even during local `docs-builder` builds.
* Snapshot assembled builds: only building commits that produced a `links.json`
  * Documentation errors in one repository won't affect all the others.
  * Resilient to repositories having build failures on their integration branches, we fall back to the last known good commit.

## Link Service

The central location where all the Link Index files are published. This is a simple S3 bucket fronted by CloudFront.

## Link Index

Each repository's branch will get in individual Link Index file in the `Link Serve` representing all linkable resources of that repository's branch.
See, for example: [Elasticsearch's links.json](https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/elasticsearch/main/links.json). 
This file is used to resolve [Outbound Crosslinks](#outbound-crosslinks) and [Inbound Crosslinks](#inbound-crosslinks).

## Link Registry

A [single file](https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/link-index.json) that contains all the [Link Index files](#link-index) for all the repositories. 
This is file is published everytime [Link Index](#link-index) files are published to the Link Service. The update is handled by an AWS Lambda function that listens to SQS triggers by S3 events.

## Outbound Crosslinks

Outbound crosslinks are links from the documentation set that's being built to others. If both repositories publish to the same `Link Service`, they can link to each other using the `<repository>://<path_to_md>` syntax.

Read more about general link syntax in the [](../syntax/links.md) section.

## Inbound Crosslinks

Inbound crosslinks are links from other documentation sets to the one that's being built. 

If both repositories publish to the same `Link Service`, they can link to each other using the `<repository>://<path_to_md>` syntax.

Read more about general link syntax in the [](../syntax/links.md) section.

Using our [Link Service](#link-service), we can validate if deletions or renames of files in the documentation set break other repositories ahead of time.
