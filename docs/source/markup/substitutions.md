---
title: Substitutions
sub:
  frontmatter_key: "Front Matter Value"
  a-key-with-dashes: "A key with dashes"
  version: 7.17.0
---

Here are some variable substitutions:

| Variable              | Defined in   |
|-----------------------|--------------|
| {{frontmatter_key}}   | Front Matter |
| {{a-key-with-dashes}} | Front Matter |

Substitutions should work in code blocks too.

```{code} sh
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-{{version}}-linux-x86_64.tar.gz
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-{{version}}-linux-x86_64.tar.gz.sha512
shasum -a 512 -c elasticsearch-{{version}}-linux-x86_64.tar.gz.sha512 <1>
tar -xzf elasticsearch-{{version}}-linux-x86_64.tar.gz
cd elasticsearch-{{version}}/ <2>
```


Here is a variable with dashes: {{a-key-with-dashes}}
