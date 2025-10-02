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
# Ensure no compilation failures -- run this to confirm the absence of build errors.
dotnet build

# Run docs-builder locally
dotnet run --project src/tooling/docs-builder 

# Run all the unit tests which complete fast.
./build.sh unit-test

# Clean all the build artifacts located in .artifacts folder
./build.sh clean

# Produce native binaries -- only call this if a change touches serialization and you are committing on behalf of the developer.
 ./build.sh publishbinaries

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

You MUST update the documentation when there are changes in the markdown syntax or rendering behaviour.

## Useful file locations

- Entry points: `src/tooling/docs-builder/Program.cs`, `src/tooling/docs-assembler/Program.cs`
- Markdown processing: `src/Elastic.Markdown/Myst/`
- Web assets: `src/Elastic.Documentation.Site/Assets/`
- Configuration schemas: `src/Elastic.Documentation.Configuration/`
- Test helpers: `tests/Elastic.Markdown.Tests/TestHelpers.cs`