# docs-builder

**ALWAYS reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

Elastic's distributed documentation tooling system built on .NET 9, consisting of:
- **docs-builder**: CLI tool for building single documentation sets or assembling or assembling multiple documentation sets
- Written in C# and F# with extensive Markdown processing capabilities

## Working Effectively

### Prerequisites and Setup
Install these in order:
```bash
# Install .NET 9.0 SDK
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0
export PATH="$HOME/.dotnet:$PATH"

# Restore .NET tools (required)
dotnet tool restore

# Install Aspire workload (required for local development)
dotnet workload install aspire
```

### Build and Test Commands
```bash
# Basic build - takes 2 minutes. NEVER CANCEL. Set timeout to 180+ seconds.
dotnet build

# Custom build system - takes 1.5 minutes. NEVER CANCEL. Set timeout to 120+ seconds.
./build.sh

# Unit tests - takes 20 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
./build.sh unit-test

# Integration tests - takes 1 minute. NEVER CANCEL. Set timeout to 120+ seconds.
# Note: May fail in sandboxed environments due to network/service dependencies
./build.sh integrate

# Clean build artifacts
./build.sh clean

# Format code - takes 1 minute. NEVER CANCEL. Set timeout to 120+ seconds.
dotnet format

# Lint/format check - takes 1 minute. NEVER CANCEL. Set timeout to 120+ seconds.
dotnet format --verify-no-changes
```

### Node.js Dependencies (Documentation Site)
```bash
cd src/Elastic.Documentation.Site

# Install dependencies - takes 15 seconds
npm ci

# Lint TypeScript/JavaScript - takes 2 seconds
npm run lint

# Build web assets - takes 10 seconds
npm run build

# Run tests
npm run test
```

### CLI Tools Usage
```bash
# Build documentation from ./docs folder - takes 15 seconds
dotnet run --project src/tooling/docs-builder

# Serve with live reload at http://localhost:3000
dotnet run --project src/tooling/docs-builder -- serve

# Get help for docs-builder
dotnet run --project src/tooling/docs-builder -- --help

# Get help for docs-assembler
dotnet run --project src/tooling/docs-builder -- assemble --help

# Validate assembler configuration - takes 15 seconds
dotnet run --project src/tooling/docs-builder -c release -- assembler navigation validate
```

### Local Development Orchestration
```bash
# Run full local environment with Aspire (requires network access)
dotnet run --project aspire

# With options for local development
dotnet run --project aspire -- --assume-cloned --skip-private-repositories

# Watch mode for continuous development - monitors file changes
./build.sh watch
```

## Validation

### Always run these before submitting changes:
```bash
# 1. Format code
dotnet format

# 2. Build successfully
./build.sh

# 3. Run unit tests
./build.sh unit-test

# 4. Lint TypeScript/JavaScript
cd src/Elastic.Documentation.Site && npm run lint
```

### Testing Changes
- **ALWAYS** test the docs-builder CLI after making changes to verify functionality
- Test both building (`dotnet run --project src/tooling/docs-builder`) and serving (`dotnet run --project src/tooling/docs-builder -- serve`)
- For markdown processing changes, build the repository's own docs to validate
- For web components, build the npm assets and check output

## Common Tasks

### Key Project Structure
```
src/
  ├── Elastic.Markdown/              # Core Markdown processing engine
  ├── tooling/
  │   ├── docs-builder/               # Main CLI application
  ├── Elastic.Documentation.Site/    # Web rendering components (TypeScript/CSS)
  └── Elastic.Documentation.Configuration/  # Configuration handling

tests/
  ├── Elastic.Markdown.Tests/        # C# unit tests
  ├── authoring/                      # F# authoring tests
  └── docs-assembler.Tests/           # Assembler tests

tests-integration/                    # Integration tests
docs/                                 # Repository documentation
config/                               # YAML configuration files
```

### Adding New Features
- **Markdown Extensions**: Add to `src/Elastic.Markdown/Myst/`
- **CLI Commands**: Extend `src/tooling/docs-builder/Commands/` 
- **Web Components**: Add to `src/Elastic.Documentation.Site/`
- **Configuration**: Modify `src/Elastic.Documentation.Configuration/`

### Important Files
- `build/Targets.fs` - F# build script definitions
- `docs-builder.sln` - Main solution file
- `global.json` - .NET SDK version (9.0.100)
- `Directory.Build.props` - MSBuild properties
- `Directory.Packages.props` - Centralized package management
- `dotnet-tools.json` - Required .NET tools

### Build Timing Expectations
- **NEVER CANCEL** builds or tests - they may take longer than expected
- Initial build: ~2 minutes
- Incremental builds: ~30 seconds
- Unit tests: ~20 seconds
- Integration tests: ~1 minute (may fail without network access)
- Format checking: ~1 minute
- TypeScript build: ~10 seconds

### Environment Notes
- Builds successfully on Ubuntu, macOS, and Windows
- Integration tests require network access (may fail in sandboxed environments)
- Uses both C# (.NET 9) and F# for different components
- TypeScript/Node.js for web asset building
- Self-hosting documentation demonstrating the tool's capabilities

### Documentation Updates
When changing markdown syntax or rendering behavior, **always** update the documentation in the `/docs/` folder as this repository is self-documenting.

## Quick Reference

```bash
# Full development setup from fresh clone
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0
export PATH="$HOME/.dotnet:$PATH"
dotnet tool restore
dotnet workload install aspire
cd src/Elastic.Documentation.Site && npm ci && cd ../..

# Build and validate changes
dotnet format
./build.sh
./build.sh unit-test
cd src/Elastic.Documentation.Site && npm run lint && npm run build

# Test CLI functionality
dotnet run --project src/tooling/docs-builder
dotnet run --project src/tooling/docs-builder -- serve
```