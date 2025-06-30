# Contribute locally

Follow these steps to contribute to Elastic docs.

* [Prerequisites](#prerequisites)
* [Install `docs-builder`](#install-docs-builder)
* [Clone a content repository](#clone-content)
* [Build the docs](#build-the-docs)
* [Write the docs](#write-docs)
* [Push your changes](#push-changes)

## Prerequisites [#prerequisites]

To write and push updates to Elastic documentation, you need the following:

1. **A code editor**: we recommend [Visual Studio Code](https://code.visualstudio.com/download)
1. **Git installed on your machine**: learn how [here](https://github.com/git-guides/install-git)
1. **A GitHub account**: sign up [here](https://github.com/)

## Install `docs-builder` [#install-docs-builder]

There are two different ways to install and run `docs-builder`:

1. Download, extract, and run the binary (recommended).
1. Clone the repository and build the binary from source.

This guide uses the first option. If you'd like to clone the repository and build from source, learn how in the [project readme](https://github.com/elastic/docs-builder?tab=readme-ov-file#docs-builder).

::::{tab-set}

:::{tab-item} macOS & Linux

1. **Download and run the install script**   

   Run this command to download and install the latest version of `docs-builder`:

   ```sh
   curl -sL https://ela.st/docs-builder-install | sh
   ```
   
   This downloads the latest binary, makes it executable, and installs it to your user PATH.
   You can optionally specify a specific version to install:

   ```sh
   DOCS_BUILDER_VERSION=0.40.0 curl -sL https://ela.st/docs-builder-install | sh
   ```

   To download and install the binary file manually, refer to [Releases](https://github.com/elastic/docs-builder/releases) on GitHub.

2. **Run docs-builder from a docs folder**

   Use the `serve` command from any docs folder to start serving the documentation at http://localhost:3000:

   ```sh
   docs-builder serve
   ```

   The path to the `docset.yml` file that you want to build can be specified with `-p`.

   ::{important}
   Run `docs-builder` without `serve` to run a full build and detect errors.
   ::

To download and install the binary file manually, refer to [Releases](https://github.com/elastic/docs-builder/releases) on GitHub. 

If you get a `Permission denied` error, make sure that you aren't trying to run a directory instead of a file. Also, grant the binary file execution permissions using `chmod +x docs-builder`.

:::

:::{tab-item} Windows

1. **Download and run the install script**   

   Run this command to download and install the latest version of `docs-builder`:

   ```powershell
   iex (New-Object System.Net.WebClient).DownloadString('https://ela.st/docs-builder-install-win')
   ```

   This downloads the latest binary, makes it executable, and installs it to your user PATH.
   You can optionally specify a specific version to install:

   ```powershell
   $env:DOCS_BUILDER_VERSION = '0.40.0'; iwr -useb https://ela.st/docs-builder-install.ps1 | iex
   ```

   To download and install the binary file manually, refer to [Releases](https://github.com/elastic/docs-builder/releases) on GitHub.

2. **Run docs-builder from a docs folder**

   Use the `serve` command from any docs folder to start serving the documentation at http://localhost:3000:

   ```sh
   docs-builder serve
   ```
   The path to the `docset.yml` file that you want to build can be specified with `-p`.
:::
::::


## Clone a content repository [#clone-content]

:::{tip}
Documentation lives in many repositories across Elastic. If you're unsure which repository to clone, you can use the "Edit this page" link on any documentation page to determine where the source file lives.
:::

In this guide, we'll clone the [`docs-content`](https://github.com/elastic/docs-content) repository. The `docs-content` repository is the home for narrative documentation at Elastic. Clone this repo to a directory of your choice:

```sh
git clone https://github.com/elastic/docs-content.git
```

## Write the docs [#write-docs]

We write docs in Markdown. Refer to our [syntax](../syntax/index.md) guide for the flavor of Markdown that we support and all of our custom directives that enable you to add a little extra pizzazz to your docs.

## Build the docs

Before pushing your changes, check that your changes build successfully.

```
docs-builder
```

The build process informs you of any critical errors or warnings. It also shows less critical issues as Hints. Make sure not to introduce any new build errors, warnings, or hints.

## Push your changes [#push-changes]

After you've made your changes locally:

* [Push your commits](https://docs.github.com/en/get-started/using-git/pushing-commits-to-a-remote-repository)
* [Open a Pull Request](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/creating-a-pull-request)

## Local preview [#local-docs-preview]

Static-site generators like docs-builder can serve docs locally. This means you can edit the source and see the result in the browser in real time.

To serve the local copy of the documentation in your browser, follow these steps:

::::::{stepper}

:::::{step} Go to the docs-builder clone location

```sh
cd docs-content
```
:::::

:::::{step} Run docs-builder

Run the `docs-builder` binary with the `serve` command to build and serve the content set to http://localhost:3000. If necessary, specify the path to the `docset.yml` file that you want to build with `-p`.

For example:

::::{tab-set}

:::{tab-item} macOS & Linux

```sh
docs-builder serve -p ./migration-test
```
:::

:::{tab-item} Windows

```powershell
docs-builder serve -p .\migration-test
```
:::
::::
:::::

:::::{step} Open the documentation in the browser
Now you should be able to view the documentation locally by navigating to http://localhost:3000.
:::::
::::::
