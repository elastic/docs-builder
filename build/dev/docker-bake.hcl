variable "IMAGE_REPOSITORY" {
  default = "docs-builder"
}

variable "IMAGE_TAG" {
  default = "local"
}

# Set CACHE_REPOSITORY in CI to share BuildKit cache between runners, for
# example: ghcr.io/elastic/docs-builder-buildcache.
variable "CACHE_REPOSITORY" {
  default = ""
}

target "common" {
  context    = "."
  dockerfile = "build/dev/Dockerfile"

  cache-from = CACHE_REPOSITORY != "" ? ["type=registry,ref=${CACHE_REPOSITORY}"] : []
  cache-to   = CACHE_REPOSITORY != "" ? ["type=registry,ref=${CACHE_REPOSITORY},mode=max"] : []
}

target "tooling" {
  inherits = ["common"]
  target   = "tooling"
  tags     = ["${IMAGE_REPOSITORY}:tooling"]
}

target "runtime" {
  inherits = ["common"]
  target   = "runtime"
  tags     = ["${IMAGE_REPOSITORY}:${IMAGE_TAG}"]
}

target "mcp" {
  context    = "."
  dockerfile = "src/api/Elastic.Documentation.Mcp.Remote/Dockerfile"
  tags       = ["${IMAGE_REPOSITORY}:mcp"]

  cache-from = CACHE_REPOSITORY != "" ? ["type=registry,ref=${CACHE_REPOSITORY}-mcp"] : []
  cache-to   = CACHE_REPOSITORY != "" ? ["type=registry,ref=${CACHE_REPOSITORY}-mcp,mode=max"] : []
}

group "all" {
  targets = ["tooling", "runtime", "mcp"]
}
