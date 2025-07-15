# Version Variables

Version are exposed during build using the `{{versions.VERSIONING_SCHEME}}` variable.

For example `stack` versioning variables are exposed as `{{versions.stack}}`.

## Specialized Suffixes.

Besides the current version, the following suffixes are available:

| Version substitution                 | result                            | purpose                                 |
|--------------------------------------|-----------------------------------|-----------------------------------------| 
| `{{versions.stack}}`                 | {{version.stack}}                 | Current version                         |
| `{{versions.stack.major_minor}}`     | {{version.stack.major_minor}}     | Current `MAJOR.MINOR`                   |
| `{{versions.stack.major_x}}`         | {{version.stack.major_x}}         | Current `MAJOR.X`                       |
| `{{versions.stack.major_component}}` | {{version.stack.major_component}} | Current major component                 |
| `{{versions.stack.next_major}}`      | {{version.stack.next_major}}      | The next major version                  |
| `{{versions.stack.next_minor}}`      | {{version.stack.next_minor}}      | The next minor version                  |
| `{{versions.stack.base}}`            | {{version.stack.base}}            | The first version on the new doc system |


## Available versioning schemes.

This is dictated by the [version.yml](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation.Configuration/versions.yml) configuration file

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
