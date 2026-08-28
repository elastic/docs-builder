COMPOSE ?= docker compose

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
	$(COMPOSE) build docs-builder serve

rebuild:
	$(COMPOSE) build --no-cache docs-builder serve

docs:
	$(COMPOSE) run --rm docs-builder build

serve:
	$(COMPOSE) up serve

serve-detached:
	$(COMPOSE) up -d serve

stop:
	$(COMPOSE) stop serve

test:
	$(COMPOSE) run --rm tests

test-markdown:
	$(COMPOSE) run --rm tests dotnet test tests/Elastic.Markdown.Tests/Elastic.Markdown.Tests.csproj

clean:
	$(COMPOSE) down --remove-orphans
