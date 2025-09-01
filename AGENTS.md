# AI Assistant Guide for docs-builder

This file contains instructions and guidance for AIs when working with the docs-builder repository.

## Repository overview

This is Elastic's distributed documentation tooling system built on .NET 9, consisting of:

- **docs-builder**: CLI tool for building single documentation sets
- **docs-assembler**: CLI tool for assembling multiple doc sets
- Written in C# and F# with extensive Markdown processing capabilities

## Essential commands

### Development
```bash
# Build the project
dotnet build

# Simulates a full release -- run this to confirm the absence of build errors
dotnet run --project build -c release

# Runs all tests
dotnet test

# Run docs-builder locally
./build.sh run -- serve

# Clean build artifacts
dotnet clean
```

### Linting and Code Quality

```bash
# Format code. Always run this when output contains formatting errors.
dotnet format

# Run specific test project
dotnet test tests/Elastic.Markdown.Tests/

# Run tests with verbose output
dotnet test --logger "console;verbosity=detailed"
```

## Key architecture Points

### Main Projects

- `src/Elastic.Markdown/` - Core Markdown processing engine
- `src/tooling/docs-builder/` - Main CLI application
- `src/tooling/docs-assembler/` - Assembly tool
- `src/Elastic.Documentation.Site/` - Web rendering components

### Testing Structure

- `tests/` - C# unit tests
- `tests/authoring/` - F# authoring tests
- `tests-integration/` - Integration tests

### Configuration

- `config/` - YAML configuration files
- `Directory.Build.props` - MSBuild properties
- `Directory.Packages.props` - Centralized package management

## Development guidelines

### Adding New Features

1. **Markdown Extensions**: Add to `src/Elastic.Markdown/Myst/`
2. **CLI Commands**: Extend `src/tooling/docs-builder/Cli/` or `docs-assembler/Cli/`
3. **Web Components**: Add to `src/Elastic.Documentation.Site/`
4. **Configuration**: Modify `src/Elastic.Documentation.Configuration/`

### Code style

- Follow existing C# and F# conventions in the codebase
- ...

### Testing requirements

- Add unit tests for new functionality
- Use F# for authoring/documentation-specific tests
- ...

### Common patterns

- ...

## Documentation

The repository is self-documenting:

- `/docs/` contains comprehensive documentation

You MUST update the documentation when making changes.

## Useful file locations

- Entry points: `src/tooling/docs-builder/Program.cs`, `src/tooling/docs-assembler/Program.cs`
- Markdown processing: `src/Elastic.Markdown/Myst/`
- Web assets: `src/Elastic.Documentation.Site/Assets/`
- Configuration schemas: `src/Elastic.Documentation.Configuration/`
- Test helpers: `tests/Elastic.Markdown.Tests/TestHelpers.cs`