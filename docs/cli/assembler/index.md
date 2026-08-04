Assembler builds bring together all isolated documentation set builds and turn them into the overall documentation that gets published.

The quickest way to run an assembler build locally is:

```bash
docs-builder assembler config init --local
docs-builder assemble --serve
```

This fetches the current configuration from the `main` branch of `docs-builder`, clones all content repositories, builds the unified site, and serves it at `http://localhost:4000`.
