# Version Variables

Version are exposed during build using the `{{versions.VERSIONING_SCHEME}}` variable.

For example `stack` versioning variables are exposed as `{{versions.stack}}`.

## Specialized Suffixes.

Besides the current version, the following suffixes are available:

| Version substitution                 | result                            | purpose                                 |
|--------------------------------------|-----------------------------------|-----------------------------------------| 
| `{{version.stack}}`                 | {{version.stack}}                 | Current version                         |
| `{{version.stack.base}}`            | {{version.stack.base}}            | The first version on the new doc system |

## Formatting

Using specialized [mutation operators](substitutions.md#mutations) versions 
can be printed in any kind of ways.


| Version substitution   | result    |
|------------------------|-----------|
| `{{version.stack| M.M}}`    |  {{version.stack|M.M}} |
| `{{version.stack.base | M }}`     | {{version.stack.base | M }} |
| `{{version.stack | M+1       | M }}` | {{version.stack | M+1 | M }} |
| `{{version.stack.base | M.M+1 }}` | {{version.stack.base | M.M+1 }} |

## Available versioning schemes.

This is dictated by the [versions.yml](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation.Configuration/versions.yml) configuration file

* `stack`
* `ece`
* `ech`
* `eck`
* `ess`
* `self`
* `ecctl`
* `curator`
* `security`
* `apm_agent_android`
* `apm_agent_ios`
* `apm_agent_dotnet`
* `apm_agent_go`
* `apm_agent_java`
* `apm_agent_node`
* `apm_agent_php`
* `apm_agent_python`
* `apm_agent_ruby`
* `apm_agent_rum`
* `edot_ios`
* `edot_android`
* `edot_dotnet`
* `edot_java`
* `edot_node`
* `edot_php`
* `edot_python`
* `edot_cf_aws`
* `edot_collector`

The following are available but should not be used. These map to serverless projects and have a fixed high version number.

* `all`
* `ech`
* `ess` (This is deprectated but was added for backwards-compatibility.)

* `serverless`
* `elasticsearch`
* `observability`
