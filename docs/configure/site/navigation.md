# `navigation.yml`

The [`navigation.yml`](https://github.com/elastic/docs-builder/blob/main/config/navigation.yml) file acts as a global navigation index. Each entry points to a `toc.yml` file, which contains the navigation tree for that section. This design allows each section/project/team to manage its own navigation independently of other content sets.

Example:

```yml
toc:
  - toc: get-started # No toc.yml indicates a docs-content directory
  - toc: extend
    children:
      - toc: kibana://extend # A toc.yml file in the Kibana repo at docs/extend
        path_prefix: extend/kibana # The URL path for built content will be elastic.co/docs/extend/kibana/FILE_PATH+NAME
```

## `toc`

The location of the table of contents file.

`repo_name://path-relative-to-toc.yml`

## `path_prefix` (optional)

The `path_prefix` configuration key specifies the URL path segment that should be prefixed to all documentation URLs.

 listing all documentation sources and the corresponding `toc.yml` file that defines their navigation.


 table of roots and specifying the location of each documentation section’s table of contents (toc.yml) file. Every entry in navigation.yml must reference a toc.yml file, which actually defines the navigation structure for that documentation section.



defines the structure and organization of the navigation menu for the Elastic documentation site. 

The file is composed of nested YAML objects, where each object represents a navigation node. Navigation nodes can be categories, sections, or links to documentation pages.




% TODO we need [`navigation.yml](https://github.com/elastic/docs-builder/blob/main/config/navigation.yml`) docs
Once the repository is added, its navigation still needs to be injected into to global site navigation.