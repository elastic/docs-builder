# Build migrated content sets for Bug Bashes

The following content sets are available for pre-migration testing:

* [elasticsearch.md](https://github.com/elastic/elasticsearch.md)
* [integration-docs.md](https://github.com/elastic/integration-docs.md)
* [kibana.md](https://github.com/elastic/kibana.md)
* [logstash.md](https://github.com/elastic/logstash.md)
* [machine-learning.md](https://github.com/elastic/machine-learning.md)
* [observability-docs.md](https://github.com/elastic/observability-docs.md)

## Local directory structure

Assuming the following directory structure:

```markdown
{GitHub_Repo_Dir}/
├── tools/
│   ├── docs-builder-mac-arm64.zip
│   └── docs-builder
├── elasticsearch.md
├── observability-docs.md
└── kibana.md
```

You can build migrated content sets on a Mac by running the following commands.

```{tip}
For other systems, see [Contribute locally](../../contribute/locally.md)
```

```bash
# move to GitHub dir
cd {GitHub_Repo_Dir}

# clone req'd repos
git clone https://github.com/elastic/elasticsearch.md.git
git clone https://github.com/elastic/observability-docs.md.git
git clone https://github.com/elastic/kibana.md.git

# move back to GitHub dir
cd {GitHub_Repo_Dir}
mkdir tools
cd tools

# mac-specific
curl -LO https://github.com/elastic/docs-builder/releases/latest/download/docs-builder-mac-arm64.zip
unzip docs-builder-mac-arm64.zip

# Build ES Guide
./docs-builder serve -p ../elasticsearch.md/docs

# Build Obs Guide
./docs-builder serve -p ../observability-docs.md/docs

# Build Kib Guide
./docs-builder serve -p ../kibana.md/docs
```