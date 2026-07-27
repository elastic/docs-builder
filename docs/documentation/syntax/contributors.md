# Contributors

The `{contributors}` directive renders a grid of contributor cards with circular avatars, names, titles, and locations. Avatars are fetched from GitHub by default, with optional image overrides.

This directive uses backtick fences with a YAML body, similar to `{applies_to}`. For maximum IDE integration, you can prefix the directive with `yaml` to enable syntax highlighting in your editor.

## Basic usage

:::::::{tab-set}
::::::{tab-item} Output
```yaml {contributors}
- gh: elastic
  name: Elastic
  title: Open source search company

- gh: github
  name: GitHub
  title: Code hosting platform
```
::::::

::::::{tab-item} Markdown
````markdown
```yaml {contributors}
- gh: elastic
  name: Elastic
  title: Open source search company

- gh: github
  name: GitHub
  title: Code hosting platform
```
````
::::::
:::::::

## Full contributor details

Each contributor entry is a YAML list item with the following properties:

:::::::{tab-set}
::::::{tab-item} Output
```yaml {contributors}
- gh: elastic
  name: Elastic
  title: Search company
  location: Distributed

- gh: github
  name: GitHub
  title: Code hosting
  location: San Francisco, CA

- gh: elastic
  name: Elastic (again)
  title: Repeated entry for demo
```
::::::

::::::{tab-item} Markdown
````markdown
```yaml {contributors}
- gh: elastic
  name: Elastic
  title: Search company
  location: Distributed

- gh: github
  name: GitHub
  title: Code hosting
  location: San Francisco, CA

- gh: elastic
  name: Elastic (again)
  title: Repeated entry for demo
```
````
::::::
:::::::

## Contributors without GitHub

The `gh` property is optional. When omitted, no avatar is fetched from GitHub, and no profile link is generated. You can still supply a custom avatar via the `image` property:

:::::::{tab-set}
::::::{tab-item} Output
```yaml {contributors}
- name: Ada Lovelace
  title: Mathematician
  location: London, UK

- gh: github
  name: GitHub
  title: Code hosting platform
```
::::::

::::::{tab-item} Markdown
````markdown
```yaml {contributors}
- name: Ada Lovelace
  title: Mathematician
  location: London, UK

- gh: github
  name: GitHub
  title: Code hosting platform
```
````
::::::
:::::::

## Grouped sections

A single `{contributors}` directive renders all its entries in one grid, automatically wrapping into rows. You do not need a separate directive for each row.

To organize contributors into labeled groups (for example, by team or department), use multiple directives with regular Markdown headings between them:

:::::::{tab-set}
::::::{tab-item} Output

### Engineering

```yaml {contributors}
- gh: elastic
  name: Alice
  title: Platform Engineer

- gh: github
  name: Bob
  title: Backend Engineer

- gh: elastic
  name: Carol
  title: Frontend Engineer

- gh: github
  name: Dave
  title: SRE

- gh: elastic
  name: Eve
  title: Data Engineer
```

### Security

```yaml {contributors}
- gh: github
  name: Frank
  title: Security Engineer

- gh: elastic
  name: Grace
  title: Security Analyst
```
::::::

::::::{tab-item} Markdown
````markdown
### Engineering

```yaml {contributors}
- gh: alice
  name: Alice
  title: Platform Engineer

- gh: bob
  name: Bob
  title: Backend Engineer

- gh: carol
  name: Carol
  title: Frontend Engineer

- gh: dave
  name: Dave
  title: SRE

- gh: eve
  name: Eve
  title: Data Engineer
```

### Security

```yaml {contributors}
- gh: frank
  name: Frank
  title: Security Engineer

- gh: grace
  name: Grace
  title: Security Analyst
```
````
::::::
:::::::

## Custom avatar image

Override the default GitHub avatar with a local image using the `image` property:

````markdown
```yaml {contributors}
- gh: theletterf
  name: Fabrizio Ferri-Benedetti
  title: Senior Technical Writer
  image: ./assets/custom-avatar.png
```
````

The image path is resolved relative to the current file, just like `{image}` directives.

## Per-contributor properties

| Property | Required | Description |
|----------|----------|-------------|
| `gh` | No | GitHub username. Used for the avatar URL and profile link. |
| `name` | Yes | Display name shown below the avatar. |
| `title` | No | Job title or role. |
| `location` | No | Geographic location. |
| `image` | No | Custom avatar image path, overriding the GitHub avatar. Supports relative paths and URLs. |
