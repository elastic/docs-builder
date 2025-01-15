# Migration Guide

How to migrate content from Asciidoc to V3.

## Migration tooling

Use the [adoc-to-md](https://github.com/elastic/adoc-to-md) conversion tool to migrate content sets from Asciidoc syntax to docs-builder syntax. Instructions to use the tool are in the readme file.

After running the migration tool, you can move and manipulate files while viewing a live preview of the content with docs-builder.

## Building migrated content sets for the bug bash

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