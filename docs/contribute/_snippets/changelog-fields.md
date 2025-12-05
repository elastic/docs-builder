
```yaml
##### Required fields #####
title:

# A required string that is a short, user-facing headline
# Max 80 characters

type: 

# A required string that contains the type of change.
# It can be one of:
# - feature: new functionality
# - enhancement: extends functionality but does not break or fix existing behavior
# - bug-fix: fixes a problem in a previous version
# - known-issue: problems that we are aware of in a given version
# - breaking-change: a change to previously-documented behavior
# - deprecation: functionality that is being removed in a later release
# - docs: major documentation updates or reorgs
# - regression: functionality that no longer works or behaves incorrectly
# - security: an advisory about potential security vulnerabilities
# - other: does not fit into any of the other categories

products:

# A required array of objects that denote the affected products and their target release.

    - product:

      # A required string with a predefined product ID used for release note routing,
      # filters, and categorization.
      
      target:

      # An optional string that facilitates pre-release doc previews.
      # For products with version releases, it contains the target version number (V.R.M).
      # For products with date releases, it contains the target release date
      # or the date the PR was merged.

      lifecycle:

      # An optional string for new features and enhancements that have a specific availability.
      # It can be one of the following:
      # - preview
      # - beta
      # - ga

##### Optional fields #####
action:

# An optional string that describes what users must do to mitigate the impact
# of a breaking change or known issue.

areas:

# An optional array of strings that denotes the parts/components/services of the
# product that are specifically affected.
# The list of valid values will vary by product.
# The docs for each release typically group the changes by type then by area.

description:

# An optional string that provides additional information about what has changed.
# Max 600 characters

feature-id:

# An optional string to associate a feature or enhancement with a unique feature flag

highlight:

# An optional boolean for items that should be included in release highlights
# or the UI to draw users' attention.

impact:

# An optional string that describes how the user's environment is/will be
# affected by a breaking change.

issues:

# An optional array of strings that contain URLs for issues that are relevant to the PR.
# They are externalized in the release docs so users can follow the links and
# understand the context.

pr: 

# An optional string that contains the pull request identifier.
# It is externalized in the release docs so users can follow the link and find more details.

subtype:

# An optional string that applies only to breaking changes and further subdivides that type.
# It can be one of:
# - api: refers to changes that break an api
# - behavioral: refers to changes the way something works
# - configuration: refers to changes that break the configuration
# - dependency: refers to changes like removing support for third-party products
# - subscription: refers to changes that break licensing behavior
# - plugin: refers to changes that break a plugin
# - security: refers to changes that break authentication, authorization, or permissions
# - other: does not fit into any of the other categories
```
