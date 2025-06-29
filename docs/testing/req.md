---
applies_to:
  stack: ga 9.1
---

# Requirements

Current version: **9.0.0**

| `applies_to`                               | result                               |
|--------------------------------------------|--------------------------------------|
| `` {applies_to}`stack: preview` ``         | {applies_to}`stack: preview`         |
| `` {applies_to}`stack: preview 8.18` ``    | {applies_to}`stack: preview 8.18`    |
| `` {applies_to}`stack: preview 9.0` ``     | {applies_to}`stack: preview 9.0`     |
| `` {applies_to}`stack: preview 9.1` ``     | {applies_to}`stack: preview 9.1`     |
| `` {applies_to}`stack: ga` ``              | {applies_to}`stack: ga`              |
| `` {applies_to}`stack: ga 8.18` ``         | {applies_to}`stack: ga 8.18`         |
| `` {applies_to}`stack: ga 9.0` ``          | {applies_to}`stack: ga 9.0`          |
| `` {applies_to}`stack: ga 9.1` ``          | {applies_to}`stack: ga 9.1`          |
| `` {applies_to}`stack: beta` ``            | {applies_to}`stack: beta`            |
| `` {applies_to}`stack: beta 8.18` ``       | {applies_to}`stack: beta 8.18`       |
| `` {applies_to}`stack: beta 9.0` ``        | {applies_to}`stack: beta 9.0`        |
| `` {applies_to}`stack: beta 9.1` ``        | {applies_to}`stack: beta 9.1`        |
| `` {applies_to}`stack: deprecated` ``      | {applies_to}`stack: deprecated`      |
| `` {applies_to}`stack: deprecated 8.18` `` | {applies_to}`stack: deprecated 8.18` |
| `` {applies_to}`stack: deprecated 9.0` ``  | {applies_to}`stack: deprecated 9.0`  |
| `` {applies_to}`stack: deprecated 9.1` ``  | {applies_to}`stack: deprecated 9.1`  |
| `` {applies_to}`stack: removed` ``         | {applies_to}`stack: removed`         |
| `` {applies_to}`stack: removed 8.18` ``    | {applies_to}`stack: removed 8.18`    |
| `` {applies_to}`stack: removed 9.0` ``     | {applies_to}`stack: removed 9.0`     |
| `` {applies_to}`stack: removed 9.1` ``     | {applies_to}`stack: removed 9.1`     |
| `` {applies_to}`stack: ` ``                | {applies_to}`stack: `                |

{applies_to}`stack: deprecated 9.1, removed 9.4`


To follow this tutorial you will need to install the following components:

- An installation of Elasticsearch, based on our hosted [Elastic Cloud](https://www.elastic.co/cloud) service (which includes a free trial period), or a self-hosted service that you run on your own computer. See the Install Elasticsearch section above for installation instructions.
- A [Python](https://python.org) interpreter. Make sure it is a recent version, such as Python 3.8 or newer.

The tutorial assumes that you have no previous knowledge of Elasticsearch or general search topics, but it expects you to have a basic familiarity with the following technologies, at least at a beginner level:

- Python development
- The [Flask](https://flask.palletsprojects.com/) web framework for Python.
- The command prompt or terminal application in your operating system.
