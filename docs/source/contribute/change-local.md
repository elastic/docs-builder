---
title: Build the docs locally
---

1. Install dependencies
2. Clone repositories
3. Make changes
4. Open a Pull Request
5. Work with CI
6. Get approvals and merge
7. View your changes live on elastic.co

Follow these instructions to get started with docs-builder on your machine.

::::{tab-set}

:::{tab-item} macOS

### macOS Installation

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

### Windows Installation

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

### Linux Installation

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

### Clone the `docs-content` Repository

Clone the `docs-content` repository to a directory of your choice:
```sh
git clone https://github.com/elastic/docs-content.git
```

### Serve the Documentation

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
