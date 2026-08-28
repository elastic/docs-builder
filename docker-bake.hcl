variable "IMAGE_REPOSITORY" {
  default = "docs-builder"
}

variable "IMAGE_TAG" {
  default = "local"
}

# Set CACHE_REPOSITORY in CI to share BuildKit cache between runners, for
# example: ghcr.io/akira28/docs-builder-buildcache.
variable "CACHE_REPOSITORY" {
  default = ""
}

target "common" {
  context    = "."
  dockerfile = "Dockerfile"

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

group "all" {
  targets = ["tooling", "runtime"]
}
