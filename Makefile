COMPOSE ?= docker compose
BAKE ?= docker buildx bake
BAKE_FILE ?= docker-bake.hcl

.PHONY: help build rebuild docs serve serve-detached stop test test-markdown clean

help:
	@echo "Available targets:"
	@echo "  make build           Build the Docker image"
	@echo "  make rebuild         Rebuild the Docker image without cache"
	@echo "  make docs            Build the current documentation set"
	@echo "  make serve           Serve docs at http://localhost:3000"
	@echo "  make serve-detached  Serve docs in the background"
	@echo "  make stop            Stop the development server"
	@echo "  make test            Run the unit-test suite in Docker"
	@echo "  make test-markdown   Run Markdown tests in Docker"
	@echo "  make clean           Stop Compose services and remove containers"

build:
	$(BAKE) --file $(BAKE_FILE) --load all

rebuild:
	$(BAKE) --file $(BAKE_FILE) --no-cache --load all

docs:
	$(BAKE) --file $(BAKE_FILE) --load runtime
	$(COMPOSE) run --rm --no-build docs-builder build

serve:
	$(BAKE) --file $(BAKE_FILE) --load tooling
	$(COMPOSE) up --no-build serve

serve-detached:
	$(BAKE) --file $(BAKE_FILE) --load tooling
	$(COMPOSE) up --no-build -d serve

stop:
	$(COMPOSE) stop serve

test:
	$(BAKE) --file $(BAKE_FILE) --load tooling
	$(COMPOSE) run --rm --no-build tests

test-markdown:
	$(BAKE) --file $(BAKE_FILE) --load tooling
	$(COMPOSE) run --rm --no-build tests dotnet test tests/Elastic.Markdown.Tests/Elastic.Markdown.Tests.csproj

clean:
	$(COMPOSE) down --remove-orphans
