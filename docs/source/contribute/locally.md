---
title: Contribute locally
---

Follow these steps to contribute to Elastic docs.
* [Step 1: Install `docs-builder`](#step-one)
* [Step 2: Clone the `docs-content` repository](#step-two)
* [Step 3: Serve the Documentation](#step-three)
* [Step 4: Open a PR](#step-three)

## Step 1: Install `docs-builder` [#step-one]

::::{tab-set}

:::{tab-item} macOS

### macOS

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
   Use the `serve` command to start serving the documentation at http://localhost:5000. The path to the docset.yml file that you want to build can be specified with `-p`:
   ```sh
   ./docs-builder serve -p ./path/to/docs
   ```

:::

:::{tab-item} Windows

### Windows

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
   Use the `serve` command to start serving the documentation at http://localhost:5000. The path to the docset.yml file that you want to build can be specified with `-p`:
   ```sh
   .\docs-builder serve -p ./path/to/docs
   ```

:::

:::{tab-item} Linux

### Linux

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
   Use the `serve` command to start serving the documentation at http://localhost:5000. The path to the docset.yml file that you want to build can be specified with `-p`:
   ```sh
   ./docs-builder serve -p ./path/to/docs
   ```

:::

::::

## Clone the `docs-content` Repository  [#step-two]

Clone the `docs-content` repository to a directory of your choice:
```sh
git clone https://github.com/elastic/docs-content.git
```

## Serve the Documentation [#step-three]

1. **Navigate to the cloned repository:**
   ```sh
   cd docs-content
   ```

2. **Run the Binary:**
   Use the `serve` command to start serving the documentation at http://localhost:5000. The path to the `docset.yml` file that you want to build can be specified with `-p`:
   ```sh
   # macOS/Linux
   ./docs-builder serve -p ./migration-test

   # Windows
   .\docs-builder serve -p .\migration-test
   ```

Now you should be able to view the documentation locally by navigating to http://localhost:5000.

## Step 4: Open a PR [#step-four]

Open a PR. Good luck.

## Step 5: View on elastic.co/docs

soon...