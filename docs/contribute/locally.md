# Contribute locally

Follow these steps to contribute to Elastic docs.

* [Prerequisites](#prerequisites)
* [Step 1: Install `docs-builder`](#step-one)
* [Step 2: Clone a content repository](#step-two)
* [Step 3: Serve the Documentation](#step-three)
* [Step 4: Write docs!](#step-four)
* [Step 5: Push your changes](#step-five)

## Prerequisites

To write and push updates to Elastic documentation, you need the following:

1. **A code editor**: we recommend [Visual Studio Code](https://code.visualstudio.com/download)
1. **Git installed on your machine**: learn how [here](https://github.com/git-guides/install-git)
1. **A GitHub account**: sign up [here](https://github.com/)

## Step 1: Install `docs-builder` [#step-one]

There are two different ways to install and run `docs-builder`:

1. Download, extract, and run the binary (recommended)
1. Clone the repository and build the binary from source

This guide uses option one. If you'd like to clone the repository and build from source, learn how in the [project readme](https://github.com/elastic/docs-builder?tab=readme-ov-file#docs-builder).

::::{tab-set}

:::{tab-item} macOS

1. **Download the Binary:**
   Download the latest macOS binary from [releases](https://github.com/elastic/docs-builder/releases/latest/):
   ```sh
   curl -LO https://github.com/elastic/docs-builder/releases/latest/download/docs-builder-mac-arm64.zip
   ```

2. **Extract the Binary:**
   Unzip the downloaded file:
   ```sh
   unzip docs-builder-mac-arm64.zip
   ```

3. **Run the Binary:**
   Use the `serve` command to start serving the documentation at http://localhost:3000. The path to the `docset.yml` file that you want to build can be specified with `-p`:
   ```sh
   ./docs-builder serve -p ./path/to/docs
   ```

:::

:::{tab-item} Windows

1. **Download the Binary:**
   Download the latest Windows binary from [releases](https://github.com/elastic/docs-builder/releases/latest/):
   ```sh
   Invoke-WebRequest -Uri https://github.com/elastic/docs-builder/releases/latest/download/docs-builder-win-x64.zip -OutFile docs-builder-win-x64.zip
   ```

2. **Extract the Binary:**
   Unzip the downloaded file. You can use tools like WinZip, 7-Zip, or the built-in Windows extraction tool.
   ```sh
   Expand-Archive -Path docs-builder-win-x64.zip -DestinationPath .
   ```

3. **Run the Binary:**
   Use the `serve` command to start serving the documentation at http://localhost:3000. The path to the `docset.yml` file that you want to build can be specified with `-p`:
   ```sh
   .\docs-builder serve -p ./path/to/docs
   ```

:::

:::{tab-item} Linux

1. **Download the Binary:**
   Download the latest Linux binary from [releases](https://github.com/elastic/docs-builder/releases/latest/):
   ```sh
   wget https://github.com/elastic/docs-builder/releases/latest/download/docs-builder-linux-x64.zip
   ```

2. **Extract the Binary:**
   Unzip the downloaded file:
   ```sh
   unzip docs-builder-linux-x64.zip
   ```

3. **Run the Binary:**
   Use the `serve` command to start serving the documentation at http://localhost:3000. The path to the `docset.yml` file that you want to build can be specified with `-p`:
   ```sh
   ./docs-builder serve -p ./path/to/docs
   ```

:::

::::

## Clone a content repository [#step-two]

:::{tip}
Documentation lives in many repositories across Elastic. If you're unsure which repository to clone, you can use the "Edit this page" link on any documentation page to determine where the source file lives.
:::

In this guide, we'll clone the [`docs-content`](https://github.com/elastic/docs-content) repository. The `docs-content` repository is the home for narrative documentation at Elastic. Clone this repo to a directory of your choice:
```sh
git clone https://github.com/elastic/docs-content.git
```

## Serve the Documentation [#step-three]

1. **Navigate to the `docs-builder` clone location:**
   ```sh
   cd docs-content
   ```

1. **Run the Binary:**
   Run the binary with the `serve` command to build and serve the content set to http://localhost:3000. Specify the path to the `docset.yml` file that you want to build with `-p`.

   For example, if `docs-builder` and `docs-content` are in the same top-level directory, you would run:
   ```sh
   # macOS/Linux
   ./docs-builder serve -p ./migration-test

   # Windows
   .\docs-builder serve -p .\migration-test
   ```

Now you should be able to view the documentation locally by navigating to http://localhost:3000.

## Step 4: Write docs [#step-four]

We write docs in markdown. See our [syntax](../syntax/index.md) guide for the flavor of markdown that we support and all of our custom directives that enable you to add a little extra pizazz to your docs.

## Step 5: Push your changes [#step-five]

After you've made your changes locally,

* [Push your commits](https://docs.github.com/en/get-started/using-git/pushing-commits-to-a-remote-repository)
* [Open a Pull Request](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/creating-a-pull-request)

## Step 5: View on elastic.co/docs

soon...
