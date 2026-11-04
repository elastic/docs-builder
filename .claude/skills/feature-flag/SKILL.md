---
name: feature-flag
description: >
  Registers a feature flag as additive-only and verifies the flag-off path
  still matches the previous default. Use whenever adding or extending a
  feature flag, editing FeatureFlags.cs, changing feature_flags in
  assembler.yml, gating a code path behind a FEATURE_* environment variable,
  or enabling a flag in an environment. Do not skip this skill for flag work
  even if the user does not name it.
---

# Feature Flag Skill

A feature flag is additive. Flag off keeps the previous default. Flag on adds new behavior only.

## When to use

Read this skill before you add any of these:

- A typed flag on `FeatureFlags`
- A `feature_flags` YAML key
- A `FEATURE_*` environment override
- An `if (flag)` around a code path

## How flags work

- Typed flags live in `src/Elastic.Documentation.Configuration/Builder/FeatureFlags.cs`
- Keys normalize to kebab-case. The env override is `FEATURE_<UPPER_SNAKE>`
- Per-environment YAML keys live under `feature_flags` in `config/assembler.yml` as `UPPER_SNAKE`
- Flags stay off unless listed. Do not enable a new flag in `prod` when you create it
- `PublishEnvironment.ToFeatureFlags()` in `src/Elastic.Documentation.Configuration/Assembler/PublishEnvironment.cs` loads YAML keys through `Set()`
- Follow the test pattern in `tests/Elastic.Documentation.Build.Tests/FeatureFlagsTests.cs`

## Steps

### 1. Additive means

Flag off equals the previous default in behavior. Flag on adds new behavior only.

Do not move, hide, relocate, or delete existing output when you introduce the flag.

New behavior is a new panel, a new build step, or a new field. It is not a relocated copy of something that already shipped.

### 2. Register the flag

1. Add a typed property on `FeatureFlags`. The default stays false.
2. Add YAML keys only in the environments that opt in.
3. Do not turn the flag on in `prod` as part of creating it.

### 3. Gate only the new path

Put the `if (flag)` around new code. Leave the existing branch as it was.

Shared markup, layout, and data wiring that the old path still needs stay outside the flag.

### 4. Verify additivity before you finish

This step is required. Do not skip it.

1. List every existing surface the change touches. Include render paths, CLI output, build steps, API payloads, mobile and desktop, and assembler vs isolated builds.
2. For each surface, compare the flag-off branch to the pre-change code or output.
3. If anything that already shipped is missing, moved, or changed when the flag is off, stop. Restore it on the default path.

### 5. Prove it with tests

- The flag defaults to false.
- Flag-off preserves existing behavior.
- Flag-on shows the new behavior.
- Sibling flags stay unchanged.
- Prod, or the default environment, does not enable the new flag.

### 6. Stop conditions

Do not ship a flag whose existing behavior lives only on the new path.

Do not treat "the new path has it" as coverage for the old path.
