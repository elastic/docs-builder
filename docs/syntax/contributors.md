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
