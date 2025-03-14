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

## Code blocks

Substitutions are supported in code blocks but are disabled by default. Enable substitutions by adding `subs=true` to the code block.

````markdown
```bash subs=true
# Your code with variables
```
````

### Code directive with subs enabled

::::{tab-set}

:::{tab-item} Output

```{code} sh subs=true
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-{{version}}-linux-x86_64.tar.gz
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-{{version}}-linux-x86_64.tar.gz.sha512
shasum -a 512 -c elasticsearch-{{version}}-linux-x86_64.tar.gz.sha512
tar -xzf elasticsearch-{{version}}-linux-x86_64.tar.gz
cd elasticsearch-{{version}}/
```

:::

:::{tab-item} Markdown

````markdown
```{code} sh subs=true
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-{{version}}-linux-x86_64.tar.gz
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-{{version}}-linux-x86_64.tar.gz.sha512
shasum -a 512 -c elasticsearch-{{version}}-linux-x86_64.tar.gz.sha512
tar -xzf elasticsearch-{{version}}-linux-x86_64.tar.gz
cd elasticsearch-{{version}}/
```
````
:::

::::


### MD code block with subs enabled

::::{tab-set}

:::{tab-item} Output

```bash subs=true
echo "{{a-global-variable}}"
```

:::

:::{tab-item} Markdown

````markdown
```bash subs=true
echo "{{a-global-variable}}"
```

````
:::

###  MD code block without subs enabled
 
::::

::::{tab-set}

:::{tab-item} Output

```bash 
echo "{{a-global-variable}}"
```

:::

:::{tab-item} Markdown

````markdown
```bash
echo "{{a-global-variable}}"
```

````
:::

::::
