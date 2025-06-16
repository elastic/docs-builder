# Legacy Docs

## Legacy Page Checker

The legacy page checker is a tool that checks if an URL exists in the legacy docs system (https://www.elastic.co/guide).
It uses a checked-in bloom filter file loaded into memory to check if an URL exists.

### How to create or update the bloom filter file

The bloom filter file is created by running the following command:

```
dotnet run --project src/tooling/docs-assembler -- legacy-docs create-bloom-bin --built-docs-dir /path/to/elastic/built-docs
```

1. The `--built-docs-dir` option is the path to the locally checked-out [elastic/built-docs](https://github.com/elastic/built-docs) repository.
