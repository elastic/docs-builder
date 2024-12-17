# docs-builder. 

You've reached the home of the latest incarnation of the documentation tooling. 

This repository is host to:

* *`docs-builder`* command line tool to generate single doc-sets (13mb native code, no dependencies)
* *`docs-assembler`* command line tool to assemble all the doc sets. (IN PROGRESS)
* `elastic/docs-builder@main` Github Action to build and validate a repositories documentation


## Command line interface

```
$ docs-builder --help
Usage: [command] [options...] [-h|--help] [--version]

Converts a source markdown folder or file to an output folder

Options:
  -p|--path <string?>        Defaults to the`{pwd}/docs` folder (Default: null)
  -o|--output <string?>      Defaults to `.artifacts/html` (Default: null)
  --path-prefix <string?>    Specifies the path prefix for urls (Default: null)
  --force <bool?>            Force a full rebuild of the destination folder (Default: null)

Commands:
  generate    Converts a source markdown folder or file to an output folder
  serve       Continuously serve a documentation folder at http://localhost:5000.
	File systems changes will be reflected without having to restart the server.
```

In the near term native code will be published by CI for the following platforms

| OS       | Architectures |
|----------|---------------|
| Windows	 | x64, Arm64    |
| Linux	   | x64, Arm64    |
| macOS    | 	x64, Arm64   |

And we'll invest time in making sure these are easily obtainable (`brew`, `winget`, `apt`)

For now you can run the tool locally through `docker run`

```bash
docker run -v "./.git:/app/.git" -v "./docs:/app/docs" -v "./.artifacts:/app/.artifacts" \
  ghcr.io/elastic/docs-builder:edge
```

This ensures `.git`/`docs` and `.artifacts` (the default output directory) are mounted.

The tool will default to incremental compilation. 
Only the changed files on subsequent runs will be compiled unless you pass `--force`
to force a new compilation.

```bash
docker run -v "./.git:/app/.git" -v "./docs:/app/docs" -v "./.artifacts:/app/.artifacts" \
  ghcr.io/elastic/docs-builder:edge --force
```

#### Live mode

Through the `serve` command you can continuously and partially compile your documentation.

```bash
docker run -v "./.git:/app/.git" -v "./docs:/app/docs" -v "./.artifacts:/app/.artifacts" \
  -p 8080:8080 ghcr.io/elastic/docs-builder:edge serve
```

Each page is compiled on demand as you browse http://localhost:8080 and is never cached so changes to files and 
navigation will always be reflected upon refresh.

Note the docker image is `linux-x86` and will be somewhat slower to invoke on OSX due to virtualization.


## Github Action

The `docs-builder` tool is available as github action. 

Since it runs from a precompiled distroless image `~25mb` it's able to execute snappy. (no need to wait for building the tool itself)


```yaml
jobs:
  docs:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Build documentation
        uses: elastic/docs-builder@main
```



### GitHub Pages

To setup the tool to publish to GitHub pages use the following configuration.  
**NOTE**: In the near feature we'll make this a dedicated single step Github ction 

```yaml
steps:
  - id: repo-basename
    run: 'echo "value=`basename ${{ github.repository }}`" >> $GITHUB_OUTPUT'
  - uses: actions/checkout@v4
  - name: Setup Pages
    id: pages
    uses: actions/configure-pages@v5.0.0
  - name: Build documentation
    uses: elastic/docs-builder@main
    with:
      prefix: '${{ steps.repo-basename.outputs.value }}'
  - name: Upload artifact
    uses: actions/upload-pages-artifact@v3.0.1
    with:
      path: .artifacts/docs/html
      
  - name: Deploy artifact
    id: deployment
    uses: actions/deploy-pages@v4.0.5 
```

Note `prefix:` is required to inject the appropiate `--path-prefix` argument to `docs-builder`

Also make sure your repository settings are set up to deploy from github actions see:

https://github.com/elastic/{your-repository}/settings/pages

---
![docs/source/_static/img/github-pages.png](docs/source/_static/img/github-pages.png)

---

## Run without docker

If you have dotnet 8 installed you can use its CLI to publish a self-contained `docs-builder` native code
binary. (On my M2 Pro mac the binary is currently 13mb)

```bash
$ dotnet publish "src/docs-builder/docs-builder.csproj" -c Release -o .artifacts/publish \
    --self-contained true /p:PublishTrimmed=true /p:PublishSingleFile=false /p:PublishAot=true -a arm64
```

**Note**: `-a` should be the machines CPU architecture

The resulting binary `./.artifacts/publish/docs-builder` will run on machines without .NET installed

# Performance

To test performance it's best to build the binary and run outside of docker:

For refence here's the `markitpy-doc` docset (50k markdown files) currently takes `14s` vs `several minutes` compared to 
existing surveyed tools
