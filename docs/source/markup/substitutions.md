---
title: Substitutions
sub:
  frontmatter_key: "Front Matter Value"
  version: 7.17.0
---

Here are some variable substitutions:

| Value               | Source       |
| ------------------- | ------------ |
| {{frontmatter_key}} | Front Matter |

Substitutions should work in code blocks too.

```{code} sh
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-{{version}}-linux-x86_64.tar.gz
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-{{version}}-linux-x86_64.tar.gz.sha512
shasum -a 512 -c elasticsearch-{{version}}-linux-x86_64.tar.gz.sha512 <1>
tar -xzf elasticsearch-{{version}}-linux-x86_64.tar.gz
cd elasticsearch-{{version}}/ <2>
```
