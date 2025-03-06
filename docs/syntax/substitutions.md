---
sub:
  frontmatter_key: "Front Matter Value"
  a-key-with-dashes: "A key with dashes"
  version: 7.17.0
---

# Substitutions

Substitutions can be defined in two places:

1. In the `frontmatter` YAML within a file.
2. Globally for all files in `docset.yml`

In both cases the yaml to define them is as followed:


```yaml
subs:
  key: value
  another-var: Another Value
```

If a substitution is defined globally it may not be redefined (shaded) in a files `frontmatter`. 
Doing so will result in a build error.

To use the variables in your files, surround them in curly brackets (`{{variable}}`).

## Example

Here are some variable substitutions:

| Variable              | Defined in   |
|-----------------------|--------------|
| {{frontmatter_key}}   | Front Matter |
| {{a-key-with-dashes}} | Front Matter |
| {{a-global-variable}} | `docset.yml` |

Substitutions should work in code blocks too.

```{code} sh
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-{{version}}-linux-x86_64.tar.gz
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-{{version}}-linux-x86_64.tar.gz.sha512
shasum -a 512 -c elasticsearch-{{version}}-linux-x86_64.tar.gz.sha512
tar -xzf elasticsearch-{{version}}-linux-x86_64.tar.gz
cd elasticsearch-{{version}}/
```

```bash 
echo "{{a-global-variable}}"
```


Here is a variable with dashes: {{a-key-with-dashes}}
