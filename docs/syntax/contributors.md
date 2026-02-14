# Contributors

The `{contributors}` directive renders a grid of contributor cards with circular avatars, names, titles, and locations. Avatars are fetched from GitHub by default, with optional image overrides.

## Basic usage

:::::::{tab-set}
::::::{tab-item} Output
:::{contributors}

- @elastic
  name: Elastic
  title: Open source search company

- @github
  name: GitHub
  title: Code hosting platform

:::
::::::

::::::{tab-item} Markdown
```markdown
:::{contributors}

- @elastic
  name: Elastic
  title: Open source search company

- @github
  name: GitHub
  title: Code hosting platform

:::
```
::::::
:::::::

## Full contributor details

Each contributor entry starts with `@github_username` and supports the following properties:

:::::::{tab-set}
::::::{tab-item} Output
:::{contributors}
:columns: 3

- @elastic
  name: Elastic
  title: Search company
  location: Distributed

- @github
  name: GitHub
  title: Code hosting
  location: San Francisco, CA

- @elastic
  name: Elastic (again)
  title: Repeated entry for demo

:::
::::::

::::::{tab-item} Markdown
```markdown
:::{contributors}
:columns: 3

- @elastic
  name: Elastic
  title: Search company
  location: Distributed

- @github
  name: GitHub
  title: Code hosting
  location: San Francisco, CA

- @elastic
  name: Elastic (again)
  title: Repeated entry for demo

:::
```
::::::
:::::::

## Custom columns

Control the number of grid columns with the `:columns:` property. The default is `4`.

:::::::{tab-set}
::::::{tab-item} Output
:::{contributors}
:columns: 2

- @elastic
  name: Elastic
  title: Open source search company
  location: Distributed

- @github
  name: GitHub
  title: Code hosting platform
  location: San Francisco, CA

:::
::::::

::::::{tab-item} Markdown
```markdown
:::{contributors}
:columns: 2

- @elastic
  name: Elastic
  title: Open source search company
  location: Distributed

- @github
  name: GitHub
  title: Code hosting platform
  location: San Francisco, CA

:::
```
::::::
:::::::

## Grouped sections

A single `{contributors}` directive renders all its entries in one grid, automatically wrapping into rows based on the `:columns:` value. You do not need a separate directive for each row.

To organize contributors into labeled groups (for example, by team or department), use multiple directives with regular Markdown headings between them:

:::::::{tab-set}
::::::{tab-item} Output

### Engineering

:::{contributors}
:columns: 4

- @elastic
  name: Alice
  title: Platform Engineer

- @github
  name: Bob
  title: Backend Engineer

- @elastic
  name: Carol
  title: Frontend Engineer

- @github
  name: Dave
  title: SRE

- @elastic
  name: Eve
  title: Data Engineer

:::

### Security

:::{contributors}
:columns: 4

- @github
  name: Frank
  title: Security Engineer

- @elastic
  name: Grace
  title: Security Analyst

:::
::::::

::::::{tab-item} Markdown
```markdown
### Engineering

:::{contributors}
:columns: 4

- @alice
  name: Alice
  title: Platform Engineer

- @bob
  name: Bob
  title: Backend Engineer

- @carol
  name: Carol
  title: Frontend Engineer

- @dave
  name: Dave
  title: SRE

- @eve
  name: Eve
  title: Data Engineer

:::

### Security

:::{contributors}
:columns: 4

- @frank
  name: Frank
  title: Security Engineer

- @grace
  name: Grace
  title: Security Analyst

:::
```
::::::
:::::::

## Custom avatar image

Override the default GitHub avatar with a local image using the `image:` property:

```markdown
:::{contributors}

- @theletterf
  name: Fabrizio Ferri-Benedetti
  title: Senior Technical Writer
  image: ./assets/custom-avatar.png

:::
```

The image path is resolved relative to the current file, just like `{image}` directives.

## Per-contributor properties

| Property | Required | Description |
|----------|----------|-------------|
| `@username` (first line) | Yes | GitHub username. Used for the avatar URL and profile link. |
| `name:` | Yes | Display name shown below the avatar. |
| `title:` | No | Job title or role. |
| `location:` | No | Geographic location. |
| `image:` | No | Custom avatar image path, overriding the GitHub avatar. Supports relative paths and URLs. |

## Directive properties

| Property | Required | Default | Description |
|----------|----------|---------|-------------|
| `:columns:` | No | `4` | Number of columns in the grid. Responsive breakpoints reduce columns on smaller screens. |
