COMPOSE ?= docker compose
BAKE ?= docker buildx bake
BAKE_FILE ?= docker-bake.hcl

.PHONY: help build rebuild tooling runtime docs serve serve-fast serve-detached stop test test-area test-markdown clean

help:
	@echo "Available targets:"
	@echo "  make build           Build the Docker image"
	@echo "  make rebuild         Rebuild the Docker image without cache"
	@echo "  make docs            Build the current documentation set"
	@echo "  make serve           Serve docs at http://localhost:3000"
	@echo "  make serve-fast      Alias for the default fast serve mode"
	@echo "  make serve-detached  Serve docs in the background"
	@echo "  make stop            Stop the development server"
	@echo "  make test            Run the unit-test suite in Docker"
	@echo "  make test-area AREA=authoring  Run one test area in Docker"
	@echo "  make test-markdown   Run Markdown tests in Docker"
	@echo "  make clean           Stop Compose services and remove containers"

build:
	$(BAKE) --file $(BAKE_FILE) --load all

rebuild:
	$(BAKE) --file $(BAKE_FILE) --no-cache --load all

tooling:
	@docker image inspect docs-builder:tooling >/dev/null 2>&1 || $(BAKE) --file $(BAKE_FILE) --load tooling

runtime:
	@docker image inspect docs-builder:local >/dev/null 2>&1 || $(BAKE) --file $(BAKE_FILE) --load runtime

docs:
	$(MAKE) runtime
	$(COMPOSE) run --rm docs-builder build

serve:
	$(MAKE) tooling
	$(COMPOSE) up --no-build serve

serve-fast: serve

serve-detached:
	$(BAKE) --file $(BAKE_FILE) --load tooling
	$(COMPOSE) up --no-build -d serve

stop:
	$(COMPOSE) stop serve

test:
	$(MAKE) tooling
	$(COMPOSE) run --rm tests

test-area:
	$(MAKE) tooling
	@set -eu; \
	case "$(AREA)" in \
		authoring) project="tests/authoring/authoring.fsproj" ;; \
		markdown) project="tests/Elastic.Markdown.Tests/Elastic.Markdown.Tests.csproj" ;; \
		configuration) project="tests/Elastic.Documentation.Configuration.Tests/Elastic.Documentation.Configuration.Tests.csproj" ;; \
		navigation) project="tests/Navigation.Tests/Navigation.Tests.csproj" ;; \
		indexing) project="tests/Elastic.Documentation.Indexing.Tests/Elastic.Documentation.Indexing.Tests.csproj" ;; \
		api-explorer) project="tests/Elastic.ApiExplorer.Tests/Elastic.ApiExplorer.Tests.csproj" ;; \
		legacy-migration) project="tests/Elastic.LegacyDocs.Migration.Tests/Elastic.LegacyDocs.Migration.Tests.csproj" ;; \
		*) echo "Unknown AREA='$(AREA)'. Use authoring, markdown, configuration, navigation, indexing, api-explorer, or legacy-migration." >&2; exit 2 ;; \
	esac; \
	$(COMPOSE) run --rm tests dotnet test -c Release "$$project" $(TEST_ARGS)

test-markdown:
	$(MAKE) tooling
	$(COMPOSE) run --rm tests dotnet test -c Release tests/Elastic.Markdown.Tests/Elastic.Markdown.Tests.csproj

clean:
	$(COMPOSE) down --remove-orphans
