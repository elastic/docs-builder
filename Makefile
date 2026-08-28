COMPOSE ?= docker compose
BAKE ?= docker buildx bake
BAKE_FILE ?= docker-bake.hcl

.PHONY: help build rebuild tooling runtime docs serve serve-fast serve-detached stop test test-markdown clean

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

test-markdown:
	$(MAKE) tooling
	$(COMPOSE) run --rm tests dotnet test tests/Elastic.Markdown.Tests/Elastic.Markdown.Tests.csproj

clean:
	$(COMPOSE) down --remove-orphans
